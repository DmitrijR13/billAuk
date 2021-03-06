--central data

drop table dep_servs;
CREATE TABLE dep_servs (
	nzp_dep_servs           serial,
        nzp_dep			INTEGER NOT NULL,	
	nzp_serv		INTEGER NOT NULL,
	nzp_serv_slave	INTEGER NOT NULL,
	nzp_area		INTEGER DEFAULT 0,
	dat_s			DATE,
	dat_po			DATE,
	is_actual		INTEGER DEFAULT 1
);

-- kernel
CREATE TABLE s_dep_types 
(
	nzp_dep		SERIAL NOT NULL,
	name_dep	VARCHAR(100)
);

INSERT INTO s_dep_types (nzp_dep, name_dep) VALUES (1, 'Перерасчет');
INSERT INTO s_dep_types (nzp_dep, name_dep) VALUES (2, 'Недопоставка');
INSERT INTO s_dep_types (nzp_dep, name_dep) VALUES (3, 'Платежный документ');