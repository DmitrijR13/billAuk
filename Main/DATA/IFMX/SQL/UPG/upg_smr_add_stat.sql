function go(nmbd);

database #nmbd_data;

--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".ins_mod_zakaz;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".ins_stat;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".mod_stat;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TABLE "are".upg_stat;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
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
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TRIGGER "are".insert_zvk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP TRIGGER "are".update_zvk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".put_upg_stat;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------

CREATE TABLE "are".upg_stat 
(
_date date,
nzp_dest integer,
nzp_supp integer,
cnt_beg integer default 0,
cnt_crt integer default 0,
cnt_crtp integer default 0,
cnt_otm integer default 0,
cnt_fact integer default 0,
cnt_plan integer default 0
);
CREATE INDEX ix_upg_stat_date on upg_stat (_date);
CREATE INDEX ix_upg_stat_dest on upg_stat (_date, nzp_dest);
CREATE INDEX ix_upg_stat_supp on upg_stat (_date, nzp_supp);
CREATE UNIQUE INDEX ix_upg_stat_uniq on upg_stat (_date, nzp_dest, nzp_supp);
------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".ins_stat (pDate date, pNzpDest integer, pNzpSupp integer)
ON EXCEPTION Return; END EXCEPTION
INSERT INTO upg_stat (_date, nzp_dest, nzp_supp) values (pDate, pNzpDest, pNzpSupp);
END PROCEDURE;
------------------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".mod_stat (pI integer, oZakazNo integer, pOrderDate date, pNzpDest integer, pNzpSupp integer, 
                                 pNzpRes integer, pFactDate date, pIsRepl integer)
DEFINE xCrt  integer;
DEFINE xCrtP integer;
DEFINE xOtm  integer;
DEFINE xFact integer;
DEFINE xPlan integer;
LET xCrt  = 0;
LET xCrtP = 0;
LET xOtm  = 0;
LET xFact = 0;
LET xPlan = 0;

IF pI>0 THEN 
   CALL ins_stat (pOrderDate, pNzpDest, pNzpSupp);
   CALL ins_stat (pFactDate, pNzpDest, pNzpSupp);
END IF;

IF oZakazNo=0 THEN
   LET xCrt=pI;
   IF pIsRepl =1 THEN  LET xCrtP=pI; END IF; 

   UPDATE upg_stat SET cnt_crt=cnt_crt+xCrt , cnt_crtp = cnt_crtp + xCrtP
   WHERE _date=pOrderDate AND
      nzp_dest=pNzpDest AND
      nzp_supp=pNzpSupp;
END IF;

IF nvl(pFactDate,0) <> 0  THEN
   IF pNzpRes=2 THEN LET xPlan = xPlan + pI; END IF;
   IF pNzpRes=3 THEN LET xFact = xFact + pI; END IF;
   IF pNzpRes=4 THEN LET xOtm  = xOtm  + pI; END IF; 
   UPDATE upg_stat SET 
          cnt_otm=cnt_otm+xOtm,
          cnt_fact=cnt_fact+xFact,
          cnt_plan=cnt_plan + xPlan
   WHERE _date=pFactDate AND
         nzp_dest=pNzpDest AND
         nzp_supp=pNzpSupp;
END IF;

END PROCEDURE; 
---------------------------------------------------------
CREATE PROCEDURE "are".ins_mod_zakaz( oZakazNo integer, nZakazNo integer, nIsRepl integer, nOrderDate date, 
                      oNzpDest integer, oNzpSupp integer, oNzpRes integer, oFactDate date, 
                      nNzpDest integer, nNzpSupp integer, nNzpRes integer, nFactDate date)

DEFINE xCurUnl integer;
IF (oZakazNo <> nZakazNo) OR
   (oNzpDest <> nNzpDest) OR
   (oNzpSupp <> nNzpSupp) OR
   (oNzpRes  <> nNzpRes) OR 
   (oFactDate <> nFactDate) 
   THEN 
       IF (oZakazNo <> 0) AND (oZakazNo IS NOT null) THEN 
       UPDATE zakaz SET  b_unload=0 WHERE  nzp_zk = nZakazNo;
       call mod_stat (-1, oZakazNo, nOrderDate, oNzpDest, oNzpSupp, oNzpRes, oFactDate, nIsRepl);
       END IF;
       call mod_stat ( 1, 0, nOrderDate, nNzpDest, nNzpSupp, nNzpRes, nFactDate, nIsRepl );
       UPDATE zakaz SET  b_unload=6 WHERE  nzp_zk = nZakazNo;
   END IF;

END PROCEDURE;
---------------------------------------------------------
create trigger "are".insert_zakaz insert on "are".zakaz 
    referencing new as new_zakaz
    for each row
        (
        execute procedure "are".mod_stat ( 
        1, 0, new_zakaz.order_date, new_zakaz.nzp_dest, new_zakaz.nzp_supp, new_zakaz.nzp_res, new_zakaz.fact_date, 
        new_zakaz.is_replicate
    ));

create trigger "are".update_zakaz update of nzp_zk, nzp_zvk,nzp_dest,
    nzp_supp,nzp_res,nzp_user,norm,exec_date,comment,
    comment_n,order_date,fact_date,is_replicate,nzp_atts,
    repeated,replno,nedop_s,act_actual,act_s,act_po,act_num_nedop,
    temperature,act_temperature,ds_actual
    on "are".zakaz referencing old as old_zakaz 
    new as new_zakaz
    for each row
        (
        execute procedure "are".ins_mod_zakaz(
                      old_zakaz.nzp_zk, new_zakaz.nzp_zk, new_zakaz.is_replicate, new_zakaz.order_date, 
                      old_zakaz.nzp_dest, old_zakaz.nzp_supp, old_zakaz.nzp_res, old_zakaz.fact_date, 
                      new_zakaz.nzp_dest, new_zakaz.nzp_supp, new_zakaz.nzp_res, new_zakaz.fact_date)
    );
--------------------------------------------------------------------------------------------------
create procedure "are".put_upg_stat(pBeg date, pZkBeg date)

create temp table t1 (
   _date DATE,
   nzp_dest INTEGER,
   nzp_supp INTEGER,
   cnt_beg INTEGER default 0,
   cnt_crt INTEGER default 0,
   cnt_crtp INTEGER default 0,
   cnt_otm INTEGER default 0,
   cnt_fact INTEGER default 0,
   cnt_plan INTEGER default 0)
with no log;


-- Не выполнено к началу периода
insert into t1 (_date, nzp_dest, nzp_supp, cnt_beg)
select pBeg, nzp_dest, nzp_supp, count(*)  
from zakaz 
where year(order_date)<year(pBeg) and order_date > pZkBeg
and 
((fact_date is null and nzp_res=5) or
 (date(fact_date)>=pBeg and nzp_res=3) or 
 (date(plan_date)>=pBeg and nzp_res=2)
 )
group by 1,2,3;
 

-- Направлено за период
insert into t1 (_date, nzp_dest, nzp_supp, cnt_crt, cnt_crtp)
select date(order_date), nzp_dest, nzp_supp, count(*),    
sum((case when nzp_zk<>norm then 1 else 0 end)) 
from zakaz where order_date >=pBeg
group by 1,2,3;  

insert into t1 (_date, nzp_dest, nzp_supp, cnt_otm, cnt_fact, cnt_plan)
select date(fact_date), nzp_dest, nzp_supp, 
-- Отклонено
sum((case when nzp_res=4 then 1 else 0 end)), 
-- Выполнено за период
sum((case when nzp_res=3 then 1 else 0 end)), 
  -- Плановый ремонт
sum((case when nzp_res=2 then 1 else 0 end)) 
from zakaz z where fact_date >=pBeg 
group by 1,2,3;  

delete from upg_stat where _date>=pBeg;
insert into upg_stat (_date, nzp_dest, nzp_supp, cnt_beg, cnt_crt, cnt_crtp, cnt_otm, cnt_fact, cnt_plan)
select _date, nzp_dest, nzp_supp, sum(cnt_beg), sum(cnt_crt), sum(cnt_crtp), sum(cnt_otm), sum(cnt_fact), sum(cnt_plan)
from t1 
group by 1,2,3;

drop table t1;
 
end procedure;

--------------------------------------------------------------------------------------------------
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
