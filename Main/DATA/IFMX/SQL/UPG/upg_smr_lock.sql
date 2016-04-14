function go(nmbd);

database #nmbd_data;

--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".zvk_lock;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop TABLE "are".zk_lock;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop procedure lock_zvk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------
CREATE PROCEDURE val1() ON EXCEPTION    Return;    END EXCEPTION
drop procedure lock_zk;
END PROCEDURE;
EXECUTE PROCEDURE val1();  drop PROCEDURE val1(); 
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------

create table "are".zvk_lock
(nzp_user integer,
nzp_ integer,
date_ datetime year to second);

create index "are".ix_bl_zkv_1 on "are".zvk_lock (nzp_, date_);
create index "are".ix_bl_zkv_2 on "are".zvk_lock (nzp_user, date_);
create unique index "are".ix_u_bl_zkv_1 on "are".zvk_lock (nzp_);
create unique index "are".ix_u_bl_zkv_2 on "are".zvk_lock (nzp_user);
--------------------------------------------------------

create table "are".zk_lock
(nzp_user integer,
nzp_ integer,
date_ datetime year to second);

create index "are".ix_bl_zk_1 on "are".zk_lock (nzp_, date_);
create index "are".ix_bl_zk_2 on "are".zk_lock (nzp_user, date_);
create unique index "are".ix_u_bl_zk_1 on "are".zk_lock (nzp_);
create unique index "are".ix_u_bl_zk_2 on "are".zk_lock (nzp_user);

--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------

create procedure "are".lock_zvk(pNzpUser integer, pNzp integer)
returning integer;

define xCnt integer;
define xDateMinus20Min datetime year to second;
ON EXCEPTION Return 0; END EXCEPTION

let xDateMinus20Min=(extend(current year to second,year to second)-20 units minute); 
--update zvk_lock set nzp_=-nzp_ where nzp_=pNzp and date(date_)<=xDateMinus20Min; 
delete from zvk_lock where nzp_=pNzp and date_<=xDateMinus20Min; 
-- 
update zvk_lock set nzp_=pNzp, date_=current where nzp_user=pNzpUser; 
let xCnt=dbinfo('sqlca.sqlerrd2');
if xCnt=1 then return 1; end if; 

update zvk_lock set nzp_user=pNzpUser, date_=current where nzp_=pNzp and date_<=xDateMinus20Min; 
let xCnt=dbinfo('sqlca.sqlerrd2');
if xCnt=1 then return 1; end if; 

insert into zvk_lock (nzp_user, nzp_, date_) values (pNzpUser, pNzp, current);
let xCnt=dbinfo('sqlca.sqlerrd2');
return xCnt with resume;

end procedure; 

--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
create procedure "are".lock_zk(pNzpUser integer, pNzp integer)
returning integer;

define xCnt integer;
define xDateMinus20Min datetime year to second;
ON EXCEPTION Return 0; END EXCEPTION

let xDateMinus20Min=(extend(current,year to second)-20 units minute); 
--update zk_lock set nzp_=-nzp_ where nzp_=pNzp and date(date_)<=xDateMinus20Min; 
delete from zk_lock where nzp_=pNzp and date_<=xDateMinus20Min; 
-- 
update zk_lock set nzp_=pNzp, date_=current where nzp_user=pNzpUser; 
let xCnt=dbinfo('sqlca.sqlerrd2');
if xCnt=1 then return 1; end if; 

update zk_lock set nzp_user=pNzpUser, date_=current where nzp_=pNzp and date_<=xDateMinus20Min; 
let xCnt=dbinfo('sqlca.sqlerrd2');
if xCnt=1 then return 1; end if; 

insert into zk_lock (nzp_user, nzp_, date_) values (pNzpUser, pNzp, current);
let xCnt=dbinfo('sqlca.sqlerrd2');
return xCnt with resume;

end procedure; 
--------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------
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
