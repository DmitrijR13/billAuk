SET SEARCH_PATH TO public;

UPDATE s_actions Set act_name='40 Заявление о регистрации по месту пребывания', hlp='40 Заявление о регистрации по месту пребывания' WHERE nzp_act=214;
UPDATE s_actions Set act_name='40 Поквартирная карточка', hlp='40 Поквартирная карточка' WHERE nzp_act=241;

WITH acs as (select distinct a.nzp_ash, a.sort_kod, a.cur_page, a.nzp_act as a_nzp_act, act_tip, act_dd, s.nzp_act as sact_nap_act, s.act_name as name_rep 
             FROM actions_show a Left outer join s_actions  s on (a.nzp_act=s.nzp_act) 
             WHERE (act_tip=2 and act_dd=5 and  (cur_page=135 OR cur_page=133))), 
             ord as (select *,(select count(1) from  acs ab where ab.name_rep <=acs.name_rep) as row_num FROM  acs order by row_num) 
             UPDATE actions_show ah set sort_kod=ord.row_num from ord where ord.nzp_ash=ah.nzp_ash;
