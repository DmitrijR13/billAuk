--central-fin-yy
CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
        alter table  pu_vals add nzp_counter integer;
END PROCEDURE; EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;