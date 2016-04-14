database webXX;

set encryption password "IfmxPwd2";
update s_actions set  act_name = encrypt_aes('По договорам'), hlp = encrypt_aes('выполняет выборку данных в разрезе договоров')
where nzp_act = 522;