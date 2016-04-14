SET host=192.168.229.21
   SET port=5432
   SET db=kp5
   SET PGPASSWORD=postgres
   type %1 | "C:\Program Files\PostgreSQL\9.4\bin\psql" -h 192.168.229.21 -p 5432 -U postgres -w  -d kp5 -c "copy public.%2 from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'"  
