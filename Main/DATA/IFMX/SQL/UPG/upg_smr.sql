function go(nmbd);

database #nmbd_data;


--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".readdress;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".zvk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".ins_mod_zvk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".ins_mod_zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".gen_zvk_res;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop procedure "are".replzakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop procedure "are".atts_zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TRIGGER "are".insert_zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TRIGGER "are".update_zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--------------------------------------------------------
--     1. Сообщения
--------------------------------------------------------
CREATE TABLE "are".zvk(
   nzp_zvk SERIAL NOT NULL,              
   zvk_date DATETIME YEAR to SECOND,     
   nzp_graj INTEGER,                     
   nzp_kvar INTEGER,                     
   nzp_user INTEGER,                     
   demand_name VARCHAR(50),              
   result_comment VARCHAR(255),          
   exec_date DATE,                       
   fact_date DATE,                       
   frn_no INTEGER,                       
   nzp_ztype INTEGER,                    
   nzp_res INTEGER,                      
   comment TEXT,                         
   phone CHAR(15),                       
   last_modified DATETIME YEAR to MINUTE,
   cur_unl INTEGER,                      
   mod_point INTEGER,                    
   nzp_point INTEGER);                   

CREATE UNIQUE INDEX "are".ix_u_zvk_nzp ON "are".zvk(nzp_zvk);
CREATE INDEX "are".zvk_ix_date ON "are".zvk(zvk_date);
CREATE INDEX "are".zvk_ix_flat ON "are".zvk(nzp_kvar);
CREATE INDEX "are".zvk_ix_graj ON "are".zvk(nzp_graj);
CREATE INDEX "are".zvk_ix_resu ON "are".zvk(nzp_res);
CREATE INDEX "are".zvk_ix_type ON "are".zvk(nzp_ztype);

ALTER TABLE "are".zvk ADD CONSTRAINT PRIMARY KEY 
   (nzp_zvk) CONSTRAINT "are".uzvk_nzp;

--ALTER TABLE "are".zvk ADD CONSTRAINT (FOREIGN KEY 
--   (nzp_user) REFERENCES "are".users CONSTRAINT "are".rzvk_user);

--------------------------------------------------------
--     2. Наряды-заказы
--------------------------------------------------------
CREATE TABLE "are".zakaz(
   nzp_zk SERIAL NOT NULL,                
   nzp_zvk INTEGER NOT NULL,              
   order_date DATETIME YEAR to SECOND,    
   nzp_dest INTEGER NOT NULL,             
   nzp_supp INTEGER NOT NULL,             
   nzp_user INTEGER,                      
   norm INTEGER,                          

   exec_date DATETIME YEAR to HOUR,       
   comment NVARCHAR(100),                 
   comment_n NVARCHAR(255),               
   fact_date DATETIME YEAR to HOUR,       
   nzp_plan_no INTEGER,                   
   nzp_res INTEGER,                       

   nzp_atts INTEGER default 1,            
   is_replicate INTEGER,                  
   repeated INTEGER default 0,            
   parentno integer,                      
   replno INTEGER,                        

   num_nedop INTEGER,                     
   temperature INTEGER,                   
   nedop_s DATETIME YEAR to HOUR,         


   control_date DATETIME YEAR to SECOND,  
   plan_date DATETIME YEAR to SECOND,     

   ds_actual SMALLINT DEFAULT 0,
   ds_date DATE,
   ds_user INTEGER,

   act_actual SMALLINT DEFAULT 0,         
   act_s DATETIME YEAR to HOUR,           
   act_po DATETIME YEAR to HOUR,          
   act_num_nedop INTEGER,                 
                                          
                                          
   act_temperature INTEGER,               
   nedop_reg DATE,                        
   act_unload DATE,                       

   b_unload INTEGER,                      
   last_modified DATETIME YEAR to MINUTE, 
   nzp_status smallint default 1,
   mail_date DATETIME YEAR to MINUTE,
   accept_date DATETIME YEAR to MINUTE,
   nzp_answer integer,
   cur_unl INTEGER,
   mod_point INTEGER,
   nzp_point INTEGER
   );


CREATE UNIQUE INDEX "are".i_u_zk_no ON "are".zakaz(nzp_zk);
CREATE INDEX "are".i_zk_zvk ON "are".zakaz(nzp_zvk);
CREATE INDEX "are".ix_zk_nedreg ON "are".zakaz(nedop_reg);
CREATE INDEX "are".ix_zkz_contr ON "are".zakaz(control_date);
CREATE INDEX "are".ix_zkz_plan ON "are".zakaz(plan_date);
CREATE INDEX "are".izakreplno ON "are".zakaz(replno);
CREATE INDEX "are".zk_norm ON "are".zakaz(norm);
CREATE INDEX "are".zk_dest ON "are".zakaz(nzp_dest);
CREATE INDEX "are".zk_res ON "are".zakaz(nzp_res);
CREATE INDEX "are".ix_zk_unload on "are".zakaz (b_unload);


ALTER TABLE "are".zakaz ADD CONSTRAINT PRIMARY KEY 
   (nzp_zk) CONSTRAINT "are".uZk_no;

ALTER TABLE "are".zakaz ADD CONSTRAINT (FOREIGN KEY 
   (nzp_zvk) REFERENCES "are".zvk CONSTRAINT "are".zk_fk_zvk);


--------------------------------------------------------
--     3. Переадресации
--------------------------------------------------------
CREATE TABLE "are".readdress(
   nzp_readdr SERIAL NOT NULL,
   nzp_zvk INTEGER NOT NULL,
   nzp_slug INTEGER NOT NULL,
   nzp_user INTEGER,
   _date DATETIME YEAR to SECOND,
   comment NVARCHAR(100),
   result_comment VARCHAR(255));

CREATE UNIQUE INDEX "are".i_Rd_no ON "are".readdress(nzp_readdr);
CREATE INDEX "are".i_rd_zvk ON "are".readdress(nzp_zvk);
ALTER TABLE "are".readdress ADD CONSTRAINT (FOREIGN KEY 
   (nzp_zvk) REFERENCES "are".zvk CONSTRAINT "are".rRd_zvk);

                                     
--------------------------------------------------------
--------------------------------------------------------

CREATE PROCEDURE "are".ins_mod_zvk( pZvkNo integer )
DEFINE xCurUnl integer;
--FOREACH EXECUTE PROCEDURE getcurunl() INTO xCurUnl END FOREACH;

UPDATE zvk SET
  --cur_unl=xCurUnl,
  last_modified=today
 WHERE
  nzp_zvk = pZvkNo;
END PROCEDURE;

---------------------------------------------------------
CREATE PROCEDURE "are".ins_mod_zakaz( pZakazNo integer )

DEFINE xCurUnl integer;
--FOREACH EXECUTE PROCEDURE getcurunl() INTO xCurUnl END FOREACH;

UPDATE zakaz SET
  --cur_unl=xCurUnl,
  --mod_point=(select par_value from admin where par_name='current_point'),
  --last_modified=current,
  b_unload=0
 WHERE
  nzp_zk = pZakazNo;


END PROCEDURE;

---------------------------------------------------------

create trigger "are".insert_zakaz insert on "are".zakaz 
    referencing new as new_zakaz
    for each row
        (
        execute procedure "are".ins_mod_zakaz(new_zakaz.nzp_zk 
    ));

create trigger "are".update_zakaz update of nzp_zvk,nzp_dest,
    nzp_supp,nzp_res,nzp_user,norm,exec_date,comment,
    comment_n,order_date,fact_date,is_replicate,nzp_atts,
    repeated,replno,nedop_s,act_actual,act_s,act_po,act_num_nedop,
    temperature,act_temperature,ds_actual
    on "are".zakaz referencing old as old_zakaz 
    new as new_zakaz
    for each row
        (
        execute procedure "are".ins_mod_zakaz(new_zakaz.nzp_zk 
    ));


--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".gen_zvk_res(pZvk_no integer)
RETURNING DATE as exec_date, DATE as fact_date, INTEGER as nzp_res;

DEFINE mx_exec_date DATE;
DEFINE mx_fact_date DATE;
DEFINE xExec_date DATE;
DEFINE xFact_date DATE;
DEFINE xResult_no INTEGER;
DEFINE xReaddress FLOAT;
DEFINE xPlan_no INTEGER;
DEFINE xZvkResult_no INTEGER;

DEFINE cResUndef INTEGER;
DEFINE cResPlan INTEGER;
DEFINE cResDone INTEGER;
DEFINE cResOtm INTEGER;
DEFINE cResDoing INTEGER;
DEFINE cResReaddress INTEGER;
DEFINE xReaddressCount FLOAT;
DEFINE CurResult_no INTEGER;
LET cResUndef = 1;
LET cResPlan = 2;
LET cResDone = 3;
LET cResOtm = 4;
LET cResDoing = 5;
LET cResReaddress = 6;
LET CurResult_no = cResUndef;

SELECT nzp_res , exec_date, fact_date
INTO xZvkResult_no, xExec_date, xFact_date
FROM zvk
WHERE nzp_zvk=pZvk_no;

SELECT max(exec_date), max(fact_date), max(nzp_res)
INTO mx_exec_date, mx_fact_date, xResult_no
FROM zakaz
WHERE nzp_zvk=pZvk_no AND nzp_res in (cResDone, cResDoing, cResPlan);

IF (xResult_no=cResDoing)OR(xResult_no=cResPlan) THEN LET mx_fact_date=null; END IF;

SELECT count(*)
INTO xReaddressCount
FROM readdress
WHERE readdress.nzp_zvk=pZvk_no;

IF xReaddressCount<>0 THEN LET CurResult_no = cResReaddress; END IF;
IF mx_exec_date IS NOT NULL THEN
   IF mx_fact_date IS  NULL THEN
     LET CurResult_no = xResult_no;
   ELSE
     LET CurResult_no = cResDone;
   END IF;
END IF;

IF (xZvkResult_no=cResOtm) AND (CurResult_no=cResUndef) THEN
     LET CurResult_no = cResOtm;
END IF;

UPDATE zvk
SET exec_date=mx_exec_date,
    fact_date=mx_fact_date,
    nzp_res=CurResult_no
WHERE nzp_zvk=pZvk_no;

RETURN mx_exec_date, mx_fact_date, CurResult_no;

END PROCEDURE;
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".replzakaz(pZakaz_no integer, NewZakazNo integer, pUser_no integer)
RETURNING integer;

DEFINE NewZakaz_no integer;
DEFINE xCount INTEGER;

LET xCount=0;
FOREACH SELECT count(*)
INTO xCount
FROM zakaz 
WHERE nzp_zk=pZakaz_no AND
nvl(replno,0)=0
EXIT FOREACH;
END FOREACH;

IF xCount>0 THEN

INSERT INTO Zakaz
  (nzp_zk,
  nzp_zvk,
  nzp_dest,
  nzp_supp,
  nzp_res,
  nzp_user,
  norm,
  exec_date,
  comment,
  order_date,
  repeated,
  act_num_nedop,
  act_s,
  temperature,
  act_temperature,
  nedop_s
  )
SELECT
  NewZakazNo,
  nzp_zvk,
  nzp_dest,
  nzp_supp,
  5,
  case when pUser_no=0 then nzp_user else pUser_no end,
  norm,
  exec_date,
  comment,
  current year to second,
  1,
  --case when act_num_nedop is null then num_nedop else act_num_nedop end,
  act_num_nedop,
  act_s,
  temperature,
  act_temperature,
  nedop_s
FROM zakaz z
WHERE z.nzp_zk=pzakaz_no;

LET NewZakaz_no=dbinfo('sqlca.sqlerrd1');

update zakaz set nzp_atts=3, replno=NewZakaz_no, repeated=1 where nzp_zk=pZakaz_no;
update zakaz set parentno=pZakaz_no where nzp_zk=NewZakaz_no;

update zakaz
  set act_s = exec_date
  where nzp_zk=NewZakaz_no and act_s is null;

RETURN NewZakaz_no;

ELSE
    RETURN 0;
END IF;

END PROCEDURE;
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".atts_zakaz(pZakaz_no integer, pNzpAtts integer, pNewZakaz_no integer)

DEFINE NewZakaz_no integer;
DEFINE xCount INTEGER;

if (pNzpAtts<>3) then
   if (pNzpAtts=1 or pNzpAtts=2) then
       update zakaz set nzp_atts=pNzpAtts where nzp_zk=pZakaz_no and nzp_atts<>3;
   end if;
else

SELECT count(*)
INTO xCount
FROM zakaz 
WHERE nzp_zk=pZakaz_no AND nzp_res=3 AND nvl(replno,0)=0;

IF xCount>0 THEN
execute procedure replzakaz(pZakaz_no, pNewZakaz_no, 0 ) into NewZakaz_no;
END IF;
END IF;
END PROCEDURE;
--------------------------------------------------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
alter TABLE "are".users add web_user integer default 0;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 



  
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
end function;
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------

  
--go(d1);
--go(d16);
--go(d18);
--go(d2);
--go(d26);
--go(d28);
--go(d3);
--go(d4);
--go(d6);
--go(d7);
--go(d70);
--go(d9);
go(d99);

go(d14);
go(d18);
go(d20);


