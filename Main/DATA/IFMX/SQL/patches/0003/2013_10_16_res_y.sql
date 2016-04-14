--central_kernel
delete from resolution where nzp_res = 9999;
insert into resolution (nzp_res, name_short, name_res, is_readonly) values (9999, 'ТЛС', 'таблица типов лицевых счетов', 1);

delete from res_y where nzp_res = 9999;
insert into res_y (nzp_res, nzp_y, name_y) values (9999, 1, 'население');
insert into res_y (nzp_res, nzp_y, name_y) values (9999, 2, 'бюджет');
insert into res_y (nzp_res, nzp_y, name_y) values (9999, 3, 'арендаторы');