database webXX;

set encryption password "IfmxPwd2";
update s_actions set  act_name = encrypt_aes('�� ���������'), hlp = encrypt_aes('��������� ������� ������ � ������� ���������')
where nzp_act = 522;