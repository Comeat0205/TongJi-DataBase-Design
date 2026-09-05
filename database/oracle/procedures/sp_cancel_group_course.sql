-- F · 取消团课预约（检查预约 → 修改状态 → 更新名额）
create or replace procedure sp_cancel_group_course (
   p_member_id in number,
   p_course_id in number,
   p_result    out number,
   p_message   out varchar2
) is
   v_booking_id     number;
   v_booking_status char(1);
   v_current        number;
begin
    /*
     * 锁定该会员对应的团课预约记录，
     * 防止并发取消导致重复扣减课程容量。
     */
   select booking_id,
          booking_status
     into
      v_booking_id,
      v_booking_status
     from group_course_booking
    where member_id = p_member_id
      and course_id = p_course_id
   for update;

    /*
     * 只有“已预约”状态才能取消。
     */
   if v_booking_status <> '1' then
      p_result := 0;
      if v_booking_status = '2' then
         p_message := '该预约已经取消';
      else
         p_message := '当前预约状态不允许取消';
      end if;

      return;
   end if;

    /*
     * 锁定团课记录，避免预约/取消同时修改容量。
     */
   select current_capacity
     into v_current
     from groupcourse
    where course_id = p_course_id
   for update;

    /*
     * 防止异常数据导致容量变成负数。
     */
   if v_current <= 0 then
      p_result := 0;
      p_message := '课程当前人数异常，无法取消预约';
      return;
   end if;

    /*
     * 修改预约状态：
     * 1 = 已预约
     * 2 = 已取消
     */
   update group_course_booking
      set
      booking_status = '2'
    where booking_id = v_booking_id;

    /*
     * 释放一个课程名额。
     */
   update groupcourse
      set
      current_capacity = v_current - 1
    where course_id = p_course_id;

   commit;
   p_result := 1;
   p_message := '取消预约成功';
exception
   when no_data_found then
      rollback;
      p_result := 0;
      p_message := '未找到该团课预约';
   when others then
      rollback;
      p_result := 0;
      p_message := sqlerrm;
end;
/