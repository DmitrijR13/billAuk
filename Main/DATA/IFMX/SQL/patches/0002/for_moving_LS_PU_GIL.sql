--central_data

--DROP TABLE "are".moving_operations; 
CREATE TABLE "are".moving_operations(
   id SERIAL NOT NULL,
   created_by INTEGER,
   created_on DATETIME YEAR to SECOND,
   operation_type_id INTEGER
   ) LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_moving_operations_1 ON "are".moving_operations(id);

--central_kernel
--DROP TABLE "are".moving_types; 
CREATE TABLE "are".moving_types(
   id SERIAL NOT NULL,
   type_name CHAR(25)
   ) LOCK MODE ROW; 

CREATE UNIQUE INDEX "are".ix_moving_types_1 ON "are".moving_types(id);
insert into moving_types(id, type_name) values (1, 'Смена УК');

--central_kernel
--DROP TABLE "are".object_types;  
CREATE TABLE "are".object_types(
   id SERIAL NOT NULL,
   type_name CHAR(25)
   ) LOCK MODE ROW; 

CREATE UNIQUE INDEX "are".ix_object_types_1 ON "are".object_types(id);  
insert into object_types (id, type_name) values (1, 'Лицевой счет');
insert into object_types(id, type_name) values (2, 'Прибор учета');
insert into object_types(id, type_name) values (3, 'Жилец');
insert into object_types(id, type_name) values (4, 'Собственник');
 
--local_data
--DROP TABLE "are".moving_objects; 
CREATE TABLE "are".moving_objects(
   id SERIAL NOT NULL,
   operation_id INTEGER,
   old_id INTEGER,
   new_id INTEGER,
   object_type_id INTEGER
   ) LOCK MODE ROW; 
CREATE UNIQUE INDEX "are".ix_moving_objects_1 ON "are".moving_objects(id);
