-- 说明：业务运行时由后端 EF（GroupCourseBookingAppService）实现预约扣次。
-- 本过程与当前表结构对齐（GROUP_COURSE_BOOKING.PACKAGE_ID，无 COURSE_ID），供库端演示/对照。
create or replace procedure sp_book_group_course (
   p_member_id  in number,
   p_package_id in number,
   p_course_id  in number,
   p_booking_id in number,
   p_result     out number,
   p_message    out varchar2
) is
   v_current           number;
   v_max               number;
   v_course_type_id    number;
   v_pkg_member_id     number;
   v_pkg_course_id     number;
   v_pkg_type_id       number;
   v_remaining         number;
   v_pkg_status        char(1);
   v_exist_booking_id  number;
begin
   select current_capacity,
          max_capacity,
          type_id
     into
      v_current,
      v_max,
      v_course_type_id
     from groupcourse
    where course_id = p_course_id
   for update;

   if v_current >= v_max then
      p_result := 0;
      p_message := '课程已满';
      return;
   end if;

   select member_id,
          course_id,
          remaining_count,
          package_status
     into
      v_pkg_member_id,
      v_pkg_course_id,
      v_remaining,
      v_pkg_status
     from grouppackage
    where package_id = p_package_id
   for update;

   if v_pkg_member_id <> p_member_id then
      p_result := 0;
      p_message := '课包不属于当前会员';
      return;
   end if;

   if v_pkg_status <> '1' or v_remaining <= 0 then
      p_result := 0;
      p_message := '课包不可用或剩余次数不足';
      return;
   end if;

   select type_id
     into v_pkg_type_id
     from groupcourse
    where course_id = v_pkg_course_id;

   if v_pkg_type_id <> v_course_type_id then
      p_result := 0;
      p_message := '该课包不适用于此课程类型';
      return;
   end if;

   begin
      select b.booking_id
        into v_exist_booking_id
        from group_course_booking b
        join grouppackage p on p.package_id = b.package_id
       where b.member_id = p_member_id
         and b.booking_status = '1'
         and p.course_id = p_course_id
         and rownum = 1;

      p_result := 0;
      p_message := '您已经预约该课程';
      return;
   exception
      when no_data_found then
         null;
   end;

   insert into group_course_booking (
      booking_id,
      member_id,
      package_id,
      booking_time,
      booking_status
   ) values (
      p_booking_id,
      p_member_id,
      p_package_id,
      sysdate,
      '1'
   );

   update grouppackage
      set remaining_count = remaining_count - 1,
          package_status = case
             when remaining_count - 1 <= 0 then '0'
             else package_status
          end
    where package_id = p_package_id;

   update groupcourse
      set current_capacity = v_current + 1
    where course_id = p_course_id;

   commit;
   p_result := 1;
   p_message := '预约成功，已扣除课包 1 次';

exception
   when no_data_found then
      rollback;
      p_result := 0;
      p_message := '团课或课包不存在';
   when others then
      rollback;
      p_result := 0;
      p_message := sqlerrm;
end;
/
