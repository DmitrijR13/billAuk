SET host=LinePG
   SET port=5432
   SET db=aksubaevo
   SET PGPASSWORD=postgres
   type %1 | "C:\\Program Files (x86)\\pgAdmin III\\1.18\\psql" -h LinePG -p 5432 -U postgres -w  -d aksubaevo -c "copy public.%2 from STDIN with delimiter as '|' null as '' encoding 'WIN-1251'"  
