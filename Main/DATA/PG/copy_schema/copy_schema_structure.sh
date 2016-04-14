if [ -z "$1" ] || [ -z "$2" ]
then
  echo "Usage: copy_schema_structure.sh old_schema new_schema"
  exit -1
else
  old_schema=$1
  new_schema=$2
fi
tmp_file='./fsmr_fin_13.sql'
host='127.0.0.1'
port='5432'
user='postgres'
dbname='websmr'
PGPASSWORD=postgres
export PGPASSWORD

pg_dump --host $host --port $port --username $user --role $role --format plain --schema-only --file $tmp_file --schema $old_schema $dbname
python replace_schema.py $tmp_file $old_schema $new_schema
psql --host $host --port $port --username $user --file $tmp_file -d $dbname

unset PGPASSWORD