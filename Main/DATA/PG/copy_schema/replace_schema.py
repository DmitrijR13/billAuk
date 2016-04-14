#coding:utf-8
import sys
import os


def main():
    # имя файла скрипта
    src_file_path = sys.argv[1]
    # старое имя схемы
    old_schema_name = sys.argv[2]
    # новое имя схемы
    new_schema_name = sys.argv[3]
    # заменяемые комбинации
    replace_list = [
        'CREATE SCHEMA %s;',
        'ALTER SCHEMA %s OWNER',
        'SET search_path = %s,',
        'ALTER FUNCTION %s.',
        'ALTER TABLE %s.',
    ]
    replace_map = dict((tpl % old_schema_name, tpl % new_schema_name) for tpl in replace_list)

    dirname, filename = os.path.split(src_file_path)
    # читаем весь файл
    data = open(src_file_path, 'r').readlines()
    # создаем выходной файл с таким же именем
    dst_file_path = os.path.join(dirname, filename)
    dst = open(dst_file_path, 'w')
    for line in data:
        new_line = line
        # обрабатываем строку
        for old, new in replace_map.items():
            new_line = new_line.replace(old, new)
        # пишем
        dst.write(new_line)
    dst.close()


if __name__ == "__main__":
    if len(sys.argv) != 4:
        print u'usage: replace_schema.py file.sql old_schema_name new_schema_name'
        sys.exit()
    else:
        main()
