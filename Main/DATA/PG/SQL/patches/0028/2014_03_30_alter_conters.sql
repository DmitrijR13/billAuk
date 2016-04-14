--central _data, all local _data
ALTER TABLE ftul_data.counters alter column  num_cnt type CHAR(30);
ALTER TABLE ftul_data.counters_spis alter column  num_cnt type CHAR(30);