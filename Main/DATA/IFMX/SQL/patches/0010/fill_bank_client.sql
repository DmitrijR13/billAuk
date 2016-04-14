--central_data: bc_reestr
INSERT INTO  bc_reestr(id) VALUES (-1);

--central_data: bc_reestr_files
INSERT INTO  bc_reestr_files(id) VALUES (-1);

--central_kernel: bc_types
INSERT INTO  bc_types(name_,is_active) VALUES ('iBank2',1);
INSERT INTO  bc_types(name_,is_active) VALUES ('1C ClientBank',1);
INSERT INTO  bc_types(name_,is_active) VALUES ('MW Client 32',1);

--central_kernel: bc_row_type
INSERT INTO  bc_row_type(name_) VALUES ('Заголовок файла');
INSERT INTO  bc_row_type(name_) VALUES ('Парамет платёжного поручения');
INSERT INTO  bc_row_type(name_) VALUES ('Подложка файла');

--central_kernel: bc_fields
INSERT INTO  bc_fields(name_,note_) VALUES ('SumSend','Сумма платеж');
INSERT INTO  bc_fields(name_,note_) VALUES ('DatWhen','Срок платежа');
INSERT INTO  bc_fields(name_,note_) VALUES ('Area','Наименование управляющей компании');
INSERT INTO  bc_fields(name_,note_) VALUES ('Service','Наименование услуги');
INSERT INTO  bc_fields(name_,note_) VALUES ('Dat_pp','Дата платёжного поручения');
INSERT INTO  bc_fields(name_,note_) VALUES ('Num_pp','Номер платёжного поручения');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl','Наименование плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl_T','Наименование плательщика(имя+город)');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl_INN','Наименование плательщика(ИНН+имя)');
INSERT INTO  bc_fields(name_,note_) VALUES ('INN_pl','ИНН плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('KPP_pl','КПП плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('NBankCountPl','Счёт плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePl','Наименование банка плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePl_T','Наименование банка плательщика(имя+город)');
INSERT INTO  bc_fields(name_,note_) VALUES ('Town_Pl','Город банка плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('KS_BankPl','Корсчёт банка плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('BIK_BankPl','БИК банка плательщика');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol','Наименование получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol_T','Наименование получателя(имя+город)');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol_INN','Наименование получателя(ИНН+имя)');
INSERT INTO  bc_fields(name_,note_) VALUES ('INN_pol','ИНН получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('KPP_pol','КПП получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('NBankCountPol','Счёт получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePol','Наименование банка получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePol_T','Наименование банка получателя(имя+город)');
INSERT INTO  bc_fields(name_,note_) VALUES ('Town_Pol','Город получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('KS_BankPol','Корсчёт банка получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('BIK_BankPol','БИК банка получателя');
INSERT INTO  bc_fields(name_,note_) VALUES ('PaymentDetails','Назначение платежа');
INSERT INTO  bc_fields(name_,note_) VALUES ('TypeOper','Тип операции');
INSERT INTO  bc_fields(name_,note_) VALUES ('DocType','Тип документа');
INSERT INTO  bc_fields(name_,note_) VALUES ('SendType','Тип передачи');
INSERT INTO  bc_fields(name_,note_) VALUES ('Payment','Платёжное поручение');
INSERT INTO  bc_fields(name_,note_) VALUES ('PaymentType','Вид платежа');
INSERT INTO  bc_fields(name_,note_) VALUES ('Date_cd','Дата создания файла');
INSERT INTO  bc_fields(name_,note_) VALUES ('Time_cd','Время создания файла');

--central_kernel: bc_schema
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,1,1 ,'Content-Type=doc/payment','Тип документа'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,1 ,'DATE_DOC='	 ,'Дата документа'		   ,5   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,2 ,'NUM_DOC='	 ,'Номер документа'		   ,6   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,3 ,'PAYMENT_TYPE='	 ,'Вид платежа'			   ,34  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,4 ,'PAYER_INN='	 ,'ИНН плательщика'		   ,10  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,5 ,'PAYER_NAME='	 ,'Наименование плательщика'	   ,7   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,6 ,'PAYER_ACCOUNT='	 ,'Счет плательщика'		   ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,7 ,'AMOUNT='	 ,'Сумма платежа'		   	   ,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,8 ,'PAYER_BANK_NAME=','Наименование банка плательщика'  ,13  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,9 ,'PAYER_BANK_BIC=' ,'БИК банка плательщика'	   ,17  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,10,'PAYER_BANK_ACC=' ,'Корсчет банка плательщика'	   ,16  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,11,'RCPT_INN='	 ,'ИНН получателя'		   ,21  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,12,'RCPT_NAME='	 ,'Наименование получателя'	   ,18  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,13,'RCPT_ACCOUNT='	 ,'Счет получателя'		   ,23  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,14,'RCPT_BANK_NAME=' ,'Наименование банка получателя'   ,24  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,15,'RCPT_BANK_BIC='	 ,'БИК банка получателя'	   ,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,16,'RCPT_BANK_ACC='	 ,'Корсчет банка получателя'	   ,27  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,17,'TYPE_OPER='	 ,'Вид операции'		   ,30  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,18,'QUEUE='		 ,'Очередность платежа'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,19,'PAYMENT_DETAILS=','Назначение платежа'		   ,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,20,'KPP='		 ,'КПП плательщика'		   ,11  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,21,'TERM='		 ,'Срок платежа'		   ,2   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,22,'RCPT_KPP='	 ,'КПП получателя'		   ,22  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,23,'IS_CHARGE='	 ,'Признак бюджетного платежа'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,24,'CHARGE_CREATOR=' ,'Статус составителя документа'    ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,25,'CHARGE_KBK='	 ,'Код бюджетной классификации'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,26,'CHARGE_OKATO='	 ,'Код OKATO'			   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,27,'CHARGE_BASIS='	 ,'Основание платежа'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,28,'CHARGE_PERIOD='	 ,'Налоговый период'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,29,'CHARGE_NUM_DOC=' ,'Бюджет - Номер документа'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,30,'CHARGE_DATE_DOC=','Бюджет - Дата документа'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,31,'CHARGE_TYPE='	 ,'Тип платежа'			   ,null,0,1);

INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,1 ,'1CClientBankExchange'	,'Внутренний признак файла обмена'	  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,2 ,'ВерсияФормата='	  	,'Номер версии формата обмена'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,3 ,'Кодировка='	  	,'Кодировка файла'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,4 ,'Отправитель='		,'Программа-отправитель'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,5 ,'Получатель='		,'Программа-получатель'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,6 ,'ДатаСоздания='		,'Дата формирования файла'		  ,35  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,7 ,'ВремяСоздания='		,'Время формирования файла'		  ,36  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,1 ,'ДатаНачала='		,'Дата начала интервала'		  ,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,2 ,'ДатаКонца='		,'Дата конца интервала'			  ,2   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,3 ,'РасчСчет='		,'Расчетный счет организации'	  	  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,4 ,'Документ='		,'Наименование документа'		  ,33  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,5 ,'СекцияДокумент='	,'Признак начала секции'		  	  ,33  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,6 ,'Номер='			,'Номер документа'			  ,6   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,7 ,'Дата='			,'Дата документа'			  ,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,8 ,'Сумма='			,'Сумма платежа'			  ,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,9 ,'ПлательщикСчет='	,'Расчетный счет плательщика'		  	  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,10,'ПлательщикИНН='		,'ИНН плательщика'			  ,10  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,11,'ПлательщикКПП='		,'КПП плательщика'			  ,11  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,12,'Плательщик='		,'Наименование плательщика'		  ,9   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,13,'Плательщик1='		,'Наименование плательщика'		  ,7   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,14,'ПлательщикРасчСчет='	,'Расчетный счет плательщика'		  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,15,'ПлательщикБанк1='	,'Банк плательщика'			  ,14  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,16,'ПлательщикБанк2='	,'Банк плательщика'			  ,15  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,17,'ПлательщикБИК='		,'БИК банка плательщика'		  ,17  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,18,'ПлательщикКорсчет='	,'Корсчет банка получателя'		  ,16  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,19,'ПолучательСчет='	,'Расчетный счет получателя'	 	  	  ,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,20,'ПолучательИНН='		,'ИНН получателя'			  ,21  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,21,'ПолучательКПП='		,'КПП получателя'			  ,22  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,22,'Получатель='		,'Наименование получателя'		  ,20  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,23,'Получатель1='		,'Наименование получателя'		  ,18  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,24,'ПолучательРасчСчет='	,'Расчетный счет получателя'		  ,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,25,'ПолучательБанк1='	,'Наименование банка получателя' 	  ,25  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,26,'ПолучательБанк2='	,'Наименование банка получателя'  	  ,26  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,27,'ПолучательБИК='		,'БИК банка получателя'			  ,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,28,'ПолучательКорсчет='	,'Корсчет банка получателя'		  ,27  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,29,'ВидПлатежа='		,'Вид платежа'				  ,34  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,30,'ВидОплаты='		,'Вид оплаты (тип операции)'		  ,30  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,31,'СрокПлатежа='		,'Срок платежа'				  ,2   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,32,'Очередность='		,'Очередность платежа'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,33,'СтатусСоставителя='	,'Статус составителя расчетного документа',null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,34,'НазначениеПлатежа='	,'Назначение платежа'			  ,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,35,'НазначениеПлатежа1='	,'Назначение платежа' 			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,36,'НазначениеПлатежа2='	,'Назначение платежа'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,37,'КонецДокумента'		,'Признак окончания секции'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,3,1 ,'КонецФайла'		,'Признак конца файла'			  ,null,0,1);

INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,1 ,'%START '          ,'Начала документа'			,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,2 ,'Number         : ','Номер платежного поручения'		,6   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,3 ,'Debit          : ','Расчетный счет по дебиту'		,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,4 ,'Receiver       : ','Наименование получателя'		,18  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,5 ,'Recvbank       : ','Наименование банка получателя'	,25  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,6 ,'Recvcode       : ','БИК код банка получателя'		,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,7 ,'Credit         : ','Расчётный счёт по кредиту'		,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,8 ,'Recvkorr       : ','Коррсчет получателя'			,27  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,9 ,'Recvinn        : ','ИНН получателя'			,21  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,10,'RecvKPP        : ','КПП получателя'			,22  ,0,0   );
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,11,'Ammount        : ','Сумма платежа'			,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,12,'Memo           : ','Назначение платежа'			,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,13,'Doctype        : ','Тип документа'			,31  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,14,'Sendtype       : ','Тип передачи'			,32  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,15,'Dateplat       : ','Дата платежа'			,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,16,'%STOP'            ,'Окончание документа'			,null,0,1   );




