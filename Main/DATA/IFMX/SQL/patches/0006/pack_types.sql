--CENTRAL_kernel

CREATE TABLE "are".pack_types(
   id SERIAL NOT NULL,
   type_name VARCHAR(20))
LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_pack_types_1 ON "are".pack_types(id);

insert into pack_types (id, type_name) values (10, '������ �� ���� ��');
insert into pack_types (id, type_name) values (20, '������ �� � ��');
insert into pack_types (id, type_name) values (21, '���������');