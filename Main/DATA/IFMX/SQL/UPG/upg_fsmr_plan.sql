database #pref_data;
--------------------------------------------------------
--------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop TABLE "are".act_obj;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop TABLE "are".act;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop PROCEDURE "are".mod_act;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop PROCEDURE "are".ins_act;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop trigger "are".update_act;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
drop trigger "are".insert_act;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
DROP TABLE "are".s_work_type;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION Return; END EXCEPTION
DROP TABLE "are".s_answer;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--------------------------------------------------------
--------------------------------------------------------
CREATE TABLE "are".act(
   nzp_act SERIAL NOT NULL,           
   plan_number CHAR(15),              
   plan_date DATE,                    
   nzp_supp_plant INTEGER,            
   nzp_work_type INTEGER,             
   comment NVARCHAR(255),             
   nzp_supp INTEGER ,                 
   nzp_serv INTEGER NOT NULL,         
   nzp_kind INTEGER,                  
   dat_s DATETIME YEAR to HOUR,       
   dat_po DATETIME YEAR to HOUR,      
   tn INTEGER,                        
   is_actual SMALLINT default 1,         
   _date DATE,                           
   number CHAR(15),                      
   reply_date DATE,                      
   reply_comment CHAR(255),              
   registration_date DATE default Today, 
   last_modified DATE default Today,     
   user_no INTEGER,                      
   unload_date DATE);                    
   
CREATE INDEX "are".act_date_from_ix ON "are".act(dat_s);
CREATE INDEX "are".act_date_ix ON "are".act(_date);
CREATE INDEX "are".act_date_to_ix ON "are".act(dat_po);
CREATE INDEX "are".act_last_mod_ix ON "are".act(last_modified);
CREATE INDEX "are".act_reg_date_ix ON "are".act(registration_date);
CREATE INDEX "are".ix_act_actl ON "are".act(is_actual);
CREATE INDEX "are".ix_act_ngil ON "are".act(nzp_supp, nzp_serv);
CREATE INDEX "are".ix_act_serv ON "are".act(nzp_serv);
CREATE INDEX "are".ix_act_supp ON "are".act(nzp_supp);
CREATE INDEX "are".ix_pl_date ON "are".act(plan_date);
CREATE INDEX "are".ix_pl_numb ON "are".act(plan_number);
CREATE INDEX "are".ix_plant ON "are".act(nzp_supp_plant);
CREATE INDEX "are".ix_wtype ON "are".act(nzp_work_type);
ALTER TABLE "are".act ADD CONSTRAINT PRIMARY KEY 
   (nzp_act) CONSTRAINT "are".uix_act_nzp;

----------------------------------------------------------------------------------------------------
CREATE TABLE "are".act_obj(
   no SERIAL NOT NULL,
   nzp_act INTEGER,
   nzp_ul INTEGER NOT NULL,
   nzp_dom INTEGER NOT NULL,
   nzp_kvar INTEGER);
CREATE INDEX "are".ix_actobj_flat ON "are".act_obj(nzp_kvar);
CREATE INDEX "are".ix_actobj_house ON "are".act_obj(nzp_dom);
CREATE INDEX "are".ix_actobj_street ON "are".act_obj(nzp_ul);
--ALTER TABLE "are".act_obj ADD CONSTRAINT PRIMARY KEY 
--   (nzp_act) CONSTRAINT "are".uix_actobj_nzp; 

----------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".mod_act( pActNo integer )

DEFINE xCount integer;
UPDATE act SET last_modified=today WHERE nzp_act = pActNo;
END PROCEDURE;
---------------------------------------------------------------
create trigger "are".update_act update of nzp_supp,nzp_serv,
    dat_s,dat_po,is_actual,reply_date,
    reply_comment on "are".act referencing old as old_act 
    new as new_act
    for each row (execute procedure "are".mod_act(new_act.nzp_act ));

----------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".ins_act( pActNo integer )

DEFINE xCount integer;
UPDATE act SET
  registration_date=today,
  last_modified=today
   WHERE nzp_act = pActNo ;
END PROCEDURE;                                                                                                                                                                                                                                                      
---------------------------------------------------------------
create trigger "are".insert_act insert on "are".act 
    referencing new as new_act
    for each row
        (execute procedure "are".ins_act(new_act.nzp_act ));
                 
----------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------
CREATE TABLE "are".s_work_type(
nzp_work_type serial not null,
name_work_type char(30)
);
CREATE INDEX ix_worktype on s_work_type (nzp_work_type);
insert into s_work_type (nzp_work_type, name_work_type) values (1, 'Плановые работы');
insert into s_work_type (nzp_work_type, name_work_type) values (2, 'Аварийные работы');
insert into s_work_type (nzp_work_type, name_work_type) values (3, 'Отключения по долгам');
insert into s_work_type (nzp_work_type, name_work_type) values (4, 'Прочее');
insert into s_work_type (nzp_work_type, name_work_type) values (5, 'Капитальный ремонт');
----------------------------------------------------------------------------------------------------
CREATE TABLE "are".s_answer(
nzp_answer serial not null,
name_answer char(30)
);
CREATE INDEX ix_answer on s_answer (nzp_answer);
insert into s_answer (nzp_answer, name_answer) values (1, 'Электронная почта');
insert into s_answer (nzp_answer, name_answer) values (2, 'Телефон');
insert into s_answer (nzp_answer, name_answer) values (3, 'Наряд-заказ');
insert into s_answer (nzp_answer, name_answer) values (4, 'Другое');
insert into s_answer (nzp_answer, name_answer) values (5, 'Составлен акт');
----------------------------------------------------------------------------------------------------




