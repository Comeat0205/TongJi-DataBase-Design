-- 说明：业务运行时由后端 EF（GroupCourseBookingAppService）实现取消还次 / 候补转正扣次。
-- 本过程与当前表结构对齐（按 BOOKING_ID 取消；候补转正需同类型可用课包并扣次）。
create or replace procedure sp_cancel_group_course (
   p_booking_id in number,
   p_member_id  in number,
   p_result     out number,
   p_message    out varchar2
) is
   v_booking_status     char(1);
   v_package_id         number;
   v_course_id          number;
   v_course_type_id     number;
   v_current            number;
   v_start_time         date;
   v_queue_id           number;
   v_waiting_member_id  number;
   v_wait_package_id    number;
   v_new_booking_id     number;
begin
   select b.booking_status,
          b.package_id,
          p.course_id,
          c.type_id,
          c.current_capacity,
          nvl(tst.start_time, sysdate + 1)
     into
      v_booking_status,
      v_package_id,
      v_course_id,
      v_course_type_id,
      v_current,
      v_start_time
     from group_course_booking b
     join grouppackage p on p.package_id = b.package_id
     join groupcourse c on c.course_id = p.course_id
     left join time_slot_template tst on tst.time_slot_id = c.time_slot_id
    where b.booking_id = p_booking_id
      and b.member_id = p_member_id
   for update of b.booking_status;

   if v_booking_status <> '1' then
      p_result := 0;
      p_message := case
         when v_booking_status = '2' then '该预约已经取消'
         else '当前预约状态不允许取消'
      end;
      return;
   end if;

   -- 开课前不足 3 小时不可取消（与应用层一致）
   if v_start_time - sysdate < 3 / 24 then
      p_result := 0;
      p_message := '开课前三小时内不可取消';
      return;
   end if;

   if v_current <= 0 then
      p_result := 0;
      p_message := '课程当前人数异常，无法取消预约';
      return;
   end if;

   update group_course_booking
      set booking_status = '2'
    where booking_id = p_booking_id;

   update grouppackage
      set remaining_count = remaining_count + 1,
          package_status = case
             when package_status = '0' then '1'
             else package_status
          end
    where package_id = v_package_id;

   update groupcourse
      set current_capacity = v_current - 1
    where course_id = v_course_id;

   -- 候补转正：同课最早候补，且须有该课程类型可用课包并扣次
   begin
      select queue_id,
             member_id
        into
         v_queue_id,
         v_waiting_member_id
        from (
         select w.queue_id,
                w.member_id
           from waitingqueue w
          where w.course_id = v_course_id
            and w.queue_status = '0'
          order by w.enqueue_time asc,
                   w.queue_id asc
      )
       where rownum = 1;

      select package_id
        into v_wait_package_id
        from (
         select gp.package_id
           from grouppackage gp
           join groupcourse gc on gc.course_id = gp.course_id
          where gp.member_id = v_waiting_member_id
            and gp.package_status = '1'
            and gp.remaining_count > 0
            and gc.type_id = v_course_type_id
          order by gp.package_id asc
      )
       where rownum = 1
      for update;

      select nvl(max(booking_id), 0) + 1
        into v_new_booking_id
        from group_course_booking;

      insert into group_course_booking (
         booking_id,
         member_id,
         package_id,
         booking_time,
         booking_status
      ) values (
         v_new_booking_id,
         v_waiting_member_id,
         v_wait_package_id,
         sysdate,
         '1'
      );

      update grouppackage
         set remaining_count = remaining_count - 1,
             package_status = case
                when remaining_count - 1 <= 0 then '0'
                else package_status
             end
       where package_id = v_wait_package_id;

      update waitingqueue
         set queue_status = '1',
             notified = '1'
       where queue_id = v_queue_id;

      update groupcourse
         set current_capacity = current_capacity + 1
       where course_id = v_course_id;

      commit;
      p_result := 1;
      p_message := '取消预约成功，候补会员已自动转正并扣次';
   exception
      when no_data_found then
         commit;
         p_result := 1;
         p_message := '取消预约成功';
   end;

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
