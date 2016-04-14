function go(nmbd);

database #nmbd_data;


--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
ALTER TABLE "are".nedop_kvar  add month_calc DATE;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".gen_act_nedop; 
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE "are".gen_act_nedop(pNzp INTEGER, pNzpDom INTEGER, pNzpKvar INTEGER, pActActual INTEGER, pNzpServ INTEGER, pActNumNedop INTEGER, pActTn INTEGER, 
pActS DATETIME YEAR TO HOUR, pActPo DATETIME YEAR TO HOUR, pDatPo DATE, pNzpUser INTEGER, pToday DATE)
returning integer AS CNC_COUNT, integer AS CRT_COUNT;
DEFINE nNzpNedop INTEGER;
DEFINE nActS  DATETIME YEAR TO MINUTE;
DEFINE nActPo DATETIME YEAR TO MINUTE;
DEFINE cActPo DATETIME YEAR TO MINUTE;
DEFINE nActNumNedop INTEGER;
DEFINE nActTn INTEGER;
DEFINE nMonthCalc DATE;
DEFINE xNzpKvar INTEGER;
DEFINE xCreate INTEGER;
DEFINE xDelete INTEGER;
DEFINE xMonthCalc DATE;
DEFINE xDel INTEGER;
DEFINE xIns INTEGER;
LET xDel=0; LET xIns=0;
LET xMonthCalc= MDY(MONTH(pDatPo),1,YEAR(pDatPo)); 
FOREACH SELECT k.nzp_kvar INTO xNzpKvar FROM dom d, OUTER(kvar k)
        WHERE d.nzp_dom=pNzpDom AND d.nzp_dom=k.nzp_dom AND (pNzpKvar=0 OR k.nzp_kvar=pNzpKvar)
        LET nNzpNedop=0;
        LET nActNumNedop=0;
        FOREACH SELECT nk.nzp_nedop, nk.dat_s, nk.dat_po, nk.nzp_kind, nk.tn, nk.month_calc 
                INTO   nNzpNedop, nActS, nActPo, nActNumNedop, nActTn, nMonthCalc 
                FROM   nedop_kvar nk
                WHERE  nk.nzp_kvar=xNzpKvar AND nk.act_no=pNzp AND nk.is_actual = 14
                EXIT FOREACH;
        END FOREACH;
        LET cActPo=pActPo;
        IF pActPo>pDatPo+1 THEN 
           LET cActPo=pDatPo+1 ;
        END IF;
        LET xCreate=0; LET xDelete=0; 
        IF pActActual=2 AND pActS<pActPo THEN
           LET xCreate=1; 
        END IF;
        IF nvl(nNzpNedop,0)<>0 AND (
           (xCreate=0) OR
           (xCreate=1 AND (pActS<>nActS OR cActPo<>nActPo OR pActNumNedop<>nActNumNedop OR pActTn<>nActTn))
           )
           THEN
           LET xDelete=1;
           IF xMonthCalc=nMonthCalc THEN
              DELETE FROM nedop_kvar WHERE nzp_nedop=nNzpNedop;
           ELSE
               LET xDel = xDel +1;
               UPDATE nedop_kvar SET is_actual=100,  month_calc=xMonthCalc, dat_when=today WHERE nzp_nedop=nNzpNedop;
           END IF;
        END IF; 
        IF xCreate=1 AND 
           ((NVL(nNzpNedop,0)<>0 AND xDelete=1) OR
           (NVL(nNzpNedop,0)=0))
           THEN 
           LET xIns = xIns +1;
           INSERT INTO nedop_kvar (nzp_kvar, act_no, nzp_serv, nzp_kind, tn, dat_s, dat_po, is_actual, nzp_user, dat_when, month_calc)
           VALUES (xNzpKvar, pNzp, pNzpServ, pActNumNedop, 
                   case when nvl(pActTn,0)=0 then null else pActTn end, 
                   pActS, cActPo, 14, pNzpUser, pToday, xMonthCalc);
           DELETE FROM link_group WHERE nzp=xNzpKvar and nzp_group=199;
           INSERT INTO link_group ( nzp_group,  nzp) VALUES (199, xNzpKvar);
        END IF;
END FOREACH;
RETURN xDel, xIns;
END PROCEDURE;
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".gen_zakaz_nedop;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".gen_zakaz_nedop(pNorm integer, pNzpServ integer, pNumNedop integer, 
                       pDatPo DATE, pNzpUser INTEGER, pToday DATE)

DEFINE aActS  DATETIME YEAR TO MINUTE;
DEFINE aActPo DATETIME YEAR TO MINUTE;
DEFINE aActNumNedop INTEGER;
DEFINE aActTn INTEGER;
DEFINE aActual INTEGER;

DEFINE nNzpNedop INTEGER;
DEFINE nActS  DATETIME YEAR TO MINUTE;
DEFINE nActPo DATETIME YEAR TO MINUTE;
DEFINE nActNumNedop INTEGER;
DEFINE nActTn INTEGER;
DEFINE nMonthCalc DATE;

DEFINE cActS  DATETIME YEAR TO MINUTE;
DEFINE cActPo DATETIME YEAR TO MINUTE;
DEFINE cActNumNedop INTEGER;
DEFINE cActTn INTEGER;

DEFINE cNorm INTEGER;
DEFINE cNzpZk INTEGER;
DEFINE cNzpKvar INTEGER;
DEFINE cNzpServ INTEGER;
DEFINE cActActual INTEGER;
DEFINE cDsActual INTEGER;
DEFINE cNzpRes INTEGER;

DEFINE xCreate INTEGER;
DEFINE xDelete INTEGER;
DEFINE xInsGroup INTEGER;
DEFINE xMonthCalc DATE;

LET cNzpServ=pNzpServ;
LET xMonthCalc= MDY(MONTH(pDatPo),1,YEAR(pDatPo)); 
FOREACH SELECT zk.norm, zk.nzp_zk, z.nzp_kvar, zk.act_actual, zk.ds_actual, zk.nzp_res,
               nvl(zk.act_s,zk.nedop_s), nvl(zk.act_po,pDatPo+1), nvl(zk.act_num_nedop,pNumNedop), nvl(zk.act_temperature,zk.temperature), 
               nk.nzp_nedop, nk.dat_s, nk.dat_po, nk.nzp_kind, nk.tn, nk.month_calc 
        INTO   cNorm, cNzpZk, cNzpKvar, cActActual, cDsActual, cNzpRes,
               cActS, cActPo, cActNumNedop, cActTn, 
               nNzpNedop, nActS, nActPo, nActNumNedop, nActTn, nMonthCalc 
        FROM   zvk z, zakaz zk, outer(nedop_kvar nk)
        WHERE  pNorm=zk.norm AND NVL(zk.replno,0)=0 AND zk.nzp_res<>4 AND
               zk.nzp_zvk=z.nzp_zvk AND z.nzp_kvar=nk.nzp_kvar AND nk.act_no=zk.norm AND nk.is_actual=12 AND 
               cNzpServ<>1000

        -- Периоды недопоставок: время до часов
        LET cActS= EXTEND(cActs,  YEAR TO HOUR);  
        LET cActPo=EXTEND(cActPo, YEAR TO HOUR);  
        LET nActS= EXTEND(nActS,  YEAR TO HOUR); 
        LET nActPo=EXTEND(nActPo, YEAR TO HOUR);
        IF cActPo>pDatPo+1 THEN 
           LET cActPo=pDatPo+1 ;
        END IF;

        LET xCreate=0; LET xDelete=0; LET xInsGroup=0;
        -- Если "Выполнено" и "Формировать недопоставку (акт)" 
        IF cNzpRes=3 AND cActActual=1 AND cActS<cActPo THEN
           LET xCreate = 1; 
        END IF;
        -- Если "Не выполнено" и "Формировать недопоставку (диспетчер)" 
        IF cNzpRes=5 AND cDsActual=1 AND cActS<cActPo THEN
           LET xCreate = 1; 
        END IF;
        
        IF nNzpNedop IS NOT NULL AND (
           (xCreate=0) OR
           (xCreate=1 AND (cActS<>nActS OR cActPo<>nActPo OR cActNumNedop<>nActNumNedop OR cActTn<>nActTn))
           )
        THEN
           LET xDelete=1;
           IF xMonthCalc=nMonthCalc THEN
              DELETE FROM nedop_kvar WHERE nzp_nedop=nNzpNedop;
           ELSE
              UPDATE nedop_kvar SET is_actual=100,  month_calc=xMonthCalc, dat_when=today WHERE nzp_nedop=nNzpNedop;
              LET xInsGroup = 1;
           END IF;           
        END IF; 
                
        IF xCreate=1 AND 
           ((nNzpNedop IS NOT NULL AND xDelete=1) OR
            (nNzpNedop IS NULL))
        THEN 
           INSERT INTO nedop_kvar (nzp_kvar, act_no, nzp_serv, nzp_kind, tn, dat_s, dat_po, is_actual, nzp_user, dat_when, month_calc)
           VALUES (cNzpKvar, cNorm, cNzpServ, cActNumNedop, 
                   case when nvl(cActTn,0)=0 then null else cActTn end, 
                   cActS, cActPo, 12, pNzpUser, pToday, xMonthCalc);
           LET xInsGroup = 1;
        END IF;

        IF xInsGroup=1 THEN

           DELETE FROM link_group WHERE nzp=cNzpKvar and nzp_group=198;
           INSERT INTO link_group ( nzp_group,  nzp ) VALUES ( 198, cNzpKvar );
   
        END IF;
                                                    
        UPDATE zakaz set b_unload=1 WHERE nzp_zk=cNzpZk;

END FOREACH;

END PROCEDURE;
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
DROP PROCEDURE "are".gen_zakaz_nedop_list;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------------------------------------------------
CREATE PROCEDURE "are".gen_zakaz_nedop_list(pDatPo DATE, pNzpUser INTEGER, pToday DATE)
RETURNING INTEGER AS CRT_COUNT, INTEGER AS CNC_COUNT;

DEFINE xCntCreate INTEGER;
DEFINE xCntDelete INTEGER;
DEFINE aActS  DATETIME YEAR TO MINUTE;
DEFINE aActPo DATETIME YEAR TO MINUTE;
DEFINE aActNumNedop INTEGER;
DEFINE aActTn INTEGER;
DEFINE aActual INTEGER;

DEFINE nNzpNedop INTEGER;
DEFINE nActS  DATETIME YEAR TO MINUTE;
DEFINE nActPo DATETIME YEAR TO MINUTE;
DEFINE nActNumNedop INTEGER;
DEFINE nActTn INTEGER;
DEFINE nMonthCalc DATE;

DEFINE cActS  DATETIME YEAR TO MINUTE;
DEFINE cActPo DATETIME YEAR TO MINUTE;
DEFINE cActNumNedop INTEGER;
DEFINE cActTn INTEGER;

DEFINE cNorm INTEGER;
DEFINE cNzpZk INTEGER;
DEFINE cNzpKvar INTEGER;
DEFINE cNzpServ INTEGER;
DEFINE cActActual INTEGER;
DEFINE cDsActual INTEGER;
DEFINE cNzpRes INTEGER;

DEFINE xCreate INTEGER;
DEFINE xDelete INTEGER;
DEFINE xInsGroup INTEGER;
DEFINE xMonthCalc DATE;

LET xMonthCalc= MDY(MONTH(pDatPo),1,YEAR(pDatPo)); 
LET xCntCreate =0;
LET xCntDelete =0;
FOREACH SELECT t.norm, zk.nzp_zk, z.nzp_kvar, t.nzp_serv, zk.act_actual, zk.ds_actual, zk.nzp_res,
               nvl(zk.act_s,zk.nedop_s), nvl(zk.act_po,pDatPo+1), nvl(zk.act_num_nedop,t.num_nedop), nvl(zk.act_temperature,zk.temperature), 
               nk.nzp_nedop, nk.dat_s, nk.dat_po, nk.nzp_kind, nk.tn, nk.month_calc 
        INTO   cNorm, cNzpZk, cNzpKvar, cNzpServ, cActActual, cDsActual, cNzpRes,
               cActS, cActPo, cActNumNedop, cActTn, 
               nNzpNedop, nActS, nActPo, nActNumNedop, nActTn, nMonthCalc 
        FROM   temp_nedop t, zvk z, zakaz zk, outer(nedop_kvar nk)
        WHERE  t.norm=zk.norm AND NVL(zk.replno,0)=0 AND zk.nzp_res<>4 AND
               zk.nzp_zvk=z.nzp_zvk AND z.nzp_kvar=nk.nzp_kvar AND nk.act_no=zk.norm AND nk.is_actual=12 AND
               t.nzp_serv<>1000


        -- Периоды недопоставок: время до часов
        LET cActS= EXTEND(cActs,  YEAR TO HOUR);  
        LET cActPo=EXTEND(cActPo, YEAR TO HOUR);
        LET nActS= EXTEND(nActS,  YEAR TO HOUR); 
        LET nActPo=EXTEND(nActPo, YEAR TO HOUR);
        IF cActPo>pDatPo+1 THEN 
           LET cActPo=pDatPo+1 ;
        END IF;
        
        LET xCreate=0; LET xDelete=0;  LET xInsGroup=0;
        -- Если "Выполнено" и "Формировать недопоставку (акт)" 
        IF cNzpRes=3 AND cActActual=1 AND cActS<cActPo THEN
           LET xCreate=1; 
        END IF;
        -- Если "Не выполнено" и "Формировать недопоставку (диспетчер)" 
        IF cNzpRes=5 AND cDsActual=1 AND cActS<cActPo THEN
           LET xCreate=1; 
        END IF;
        
        IF nNzpNedop IS NOT NULL AND (
           (xCreate=0) OR
           (xCreate=1 AND (cActS<>nActS OR cActPo<>nActPo OR cActNumNedop<>nActNumNedop OR cActTn<>nActTn))
           )
        THEN
           LET xDelete=1;
           LET xCntDelete=xCntDelete+1;
           IF xMonthCalc=nMonthCalc THEN
              DELETE FROM nedop_kvar WHERE nzp_nedop=nNzpNedop;
           ELSE
              UPDATE nedop_kvar SET is_actual=100,  month_calc=xMonthCalc, dat_when=today WHERE nzp_nedop=nNzpNedop;
              LET xInsGroup=1;
           END IF;           
        END IF; 
                
        IF xCreate=1 AND 
           ((nNzpNedop IS NOT NULL AND xDelete=1) OR
            (nNzpNedop IS NULL))
        THEN 
           LET xCntCreate=xCntCreate+1;
           INSERT INTO nedop_kvar (nzp_kvar, act_no, nzp_serv, nzp_kind, tn, dat_s, dat_po, is_actual, nzp_user, dat_when, month_calc)
           VALUES (cNzpKvar, cNorm, cNzpServ, cActNumNedop, 
                   case when nvl(cActTn,0)=0 then null else cActTn end, 
                   cActS, cActPo, 12, pNzpUser, pToday, xMonthCalc);
           LET xInsGroup=1;
        END IF;
        
        IF xInsGroup=1 THEN

           DELETE FROM link_group WHERE nzp=cNzpKvar and nzp_group=198;
           INSERT INTO link_group ( nzp_group,  nzp ) VALUES ( 198, cNzpKvar );
   
        END IF;

        UPDATE zakaz set b_unload=1 WHERE nzp_zk=cNzpZk;

END FOREACH;

RETURN xCntCreate,xCntDelete;
END PROCEDURE;
--------------------------------------------------------------------------------------------------
delete from s_group where nzp_group in (199, 198);
insert into "are".s_group
 (nzp_group, ngroup, txt1, txt2)
  values (198, 'Недопоставки УПГ 5.0 (Заявки)', null, null);
insert into "are".s_group
 (nzp_group, ngroup, txt1, txt2)
  values (199, 'Недопоставки УПГ 5.0 (Акты о недопоставке)', null, null);
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
