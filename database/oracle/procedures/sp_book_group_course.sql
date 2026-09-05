create or replace procedure sp_book_group_course (
   p_member_id  in number,
   p_course_id  in number,
   p_booking_id in number,
   p_result     out number,
   p_message    out varchar2
) is
   v_current        number;
   v_max            number;
   v_booking_id     number;
   v_booking_status char(1);
begin
   select current_capacity,
          max_capacity
     into
      v_current,
      v_max
     from groupcourse
    where course_id = p_course_id
   for update;

   if v_current >= v_max then
      p_result := 0;
      p_message := '课程已满';
      return;
   end if;

   begin
      select booking_id,
             booking_status
        into
         v_booking_id,
         v_booking_status
        from group_course_booking
       where member_id = p_member_id
         and course_id = p_course_id
      for update;

      if v_booking_status = '1' then
         p_result := 0;
         p_message := '您已经预约该课程';
         return;
      end if;

      if v_booking_status = '2' then
         update group_course_booking
            set booking_status = '1',
                booking_time = sysdate
          where booking_id = v_booking_id;

         update groupcourse
            set
            current_capacity = v_current + 1
          where course_id = p_course_id;

         commit;
         p_result := 1;
         p_message := '重新预约成功';
         return;
      end if;

      p_result := 0;
      p_message := '当前预约状态不允许预约';
      return;
   exception
      when no_data_found then
         insert into group_course_booking (
            booking_id,
            member_id,
            course_id,
            booking_time,
            booking_status
         ) values
            ( p_booking_id,
              p_member_id,
              p_course_id,
              sysdate,
              '1' );

         update groupcourse
            set
            current_capacity = v_current + 1
          where course_id = p_course_id;

         commit;
         p_result := 1;
         p_message := '预约成功';
   end;

exception
   when no_data_found then
      rollback;
      p_result := 0;
      p_message := '课程不存在';
   when others then
      rollback;
      p_result := 0;
      p_message := sqlerrm;
end;
/