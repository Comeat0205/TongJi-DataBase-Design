-- F · 取消团课预约 + 候补转正
create or replace procedure sp_cancel_group_course (
   p_member_id in number,
   p_course_id in number,
   p_result    out number,
   p_message   out varchar2
) is
   v_booking_id        number;
   v_booking_status    char(1);
   v_current           number;

    -- 候补会员信息
   v_queue_id          number;
   v_waiting_member_id number;
   v_new_booking_id    number;
begin
    /*
     * 1. 查询并锁定当前会员的团课预约
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
     * 2. 只有 Confirmed(1) 状态才能取消
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
     * 3. 锁定课程记录
     */
   select current_capacity
     into v_current
     from groupcourse
    where course_id = p_course_id
   for update;

    /*
     * 4. 防止容量异常
     */
   if v_current <= 0 then
      p_result := 0;
      p_message := '课程当前人数异常，无法取消预约';
      return;
   end if;

    /*
     * 5. 当前预约改为 Cancelled(2)
     */
   update group_course_booking
      set
      booking_status = '2'
    where booking_id = v_booking_id;

    /*
     * 6. 先释放一个名额
     */
   update groupcourse
      set
      current_capacity = v_current - 1
    where course_id = p_course_id;

    /*
     * 7. 查询该课程最早进入候补队列的会员
     *
     * QUEUE_STATUS = 0 表示正在等待
     * QUEUE_ID 最小者优先
     */
   begin
      select queue_id,
             member_id
        into
         v_queue_id,
         v_waiting_member_id
        from (
         select queue_id,
                member_id
           from waitingqueue
          where course_id = p_course_id
            and queue_status = '0'
          order by enqueue_time asc,
                   queue_id asc
      )
       where rownum = 1;

        /*
         * 8. 为候补会员创建正式预约
         */
      select seq_group_course_booking.nextval
        into v_new_booking_id
        from dual;

      insert into group_course_booking (
         booking_id,
         member_id,
         course_id,
         booking_time,
         booking_status
      ) values
         ( v_new_booking_id,
           v_waiting_member_id,
           p_course_id,
           sysdate,
           '1' );

        /*
         * 9. 候补记录标记为已处理
         *
         * QUEUE_STATUS = 1：已转正
         * NOTIFIED = 1：已通知
         */
      update waitingqueue
         set queue_status = '1',
             notified = '1'
       where queue_id = v_queue_id;

        /*
         * 10. 候补会员占用刚刚释放的名额
         */
      update groupcourse
         set
         current_capacity = current_capacity + 1
       where course_id = p_course_id;

      commit;
      p_result := 1;
      p_message := '取消预约成功，候补会员已自动转正';
   exception
      when no_data_found then
            /*
             * 没有候补会员：
             * 保持释放后的名额
             */
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