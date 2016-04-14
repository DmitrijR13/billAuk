--central _data, all local _data
alter table counters modify  num_cnt CHAR(30);
alter table counters_spis modify  num_cnt CHAR(30);