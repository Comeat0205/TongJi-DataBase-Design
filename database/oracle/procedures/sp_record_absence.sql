create or replace procedure sp_record_absence (
   p_booking_id  in number,
   p_course_date in date,
   p_result      out number,
   p_message     out varchar2
) is
   v_member_id      number;
   v_course_id      number;
   v_booking_status char(1);
   v_count          number;
   v_absence_id     number;
   v_instance_count number;
begin

    /*
     * 1. 查询预约记录
     */
   begin
      select member_id,
             course_id,
             booking_status
        into
         v_member_id,
         v_course_id,
         v_booking_status
        from group_course_booking
       where booking_id = p_booking_id;

   exception
      when no_data_found then
         p_result := 0;
         p_message := '预约记录不存在';
         return;
   end;


    /*
     * 2. 只有正式预约才能登记缺席
     */
   if v_booking_status <> '1' then
      p_result := 0;
      p_message := '该预约不是有效的正式预约，无法登记缺席';
      return;
   end if;


    /*
     * 3. 验证课程日期是否属于该课程
     */
   select count(*)
     into v_instance_count
     from groupcourse gc
     join time_slot_instance tsi
   on gc.time_slot_id = tsi.time_slot_id
    where gc.course_id = v_course_id
      and trunc(tsi.course_date) = trunc(p_course_date);


   if v_instance_count = 0 then
      p_result := 0;
      p_message := '该日期不是此团课的有效课程日期';
      return;
   end if;


    /*
     * 4. 防止同一预约同一天重复登记缺席
     */
   select count(*)
     into v_count
     from absencerecord
    where booking_id = p_booking_id
      and trunc(course_date) = trunc(p_course_date);


   if v_count > 0 then
      p_result := 0;
      p_message := '该预约在此课程日期已经登记过缺席';
      return;
   end if;


    /*
     * 5. 获取缺席记录主键
     */
   select seq_absencerecord.nextval
     into v_absence_id
     from dual;


    /*
     * 6. 写入缺席记录
     */
   insert into absencerecord (
      absence_id,
      member_id,
      booking_id,
      course_date,
      absence_time
   ) values
      ( v_absence_id,
        v_member_id,
        p_booking_id,
        trunc(p_course_date),
        sysdate );


   commit;
   p_result := 1;
   p_message := '缺席记录登记成功';
exception
   when others then
      rollback;
      p_result := 0;
      p_message := sqlerrm;
end;