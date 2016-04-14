--central_data: bc_reestr
INSERT INTO  bc_reestr(id) VALUES (-1);

--central_data: bc_reestr_files
INSERT INTO  bc_reestr_files(id) VALUES (-1);

--central_kernel: bc_types
INSERT INTO  bc_types(name_,is_active) VALUES ('iBank2',1);
INSERT INTO  bc_types(name_,is_active) VALUES ('1C ClientBank',1);
INSERT INTO  bc_types(name_,is_active) VALUES ('MW Client 32',1);

--central_kernel: bc_row_type
INSERT INTO  bc_row_type(name_) VALUES ('��������� �����');
INSERT INTO  bc_row_type(name_) VALUES ('������� ��������� ���������');
INSERT INTO  bc_row_type(name_) VALUES ('�������� �����');

--central_kernel: bc_fields
INSERT INTO  bc_fields(name_,note_) VALUES ('SumSend','����� ������');
INSERT INTO  bc_fields(name_,note_) VALUES ('DatWhen','���� �������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Area','������������ ����������� ��������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Service','������������ ������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Dat_pp','���� ��������� ���������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Num_pp','����� ��������� ���������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl','������������ �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl_T','������������ �����������(���+�����)');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePl_INN','������������ �����������(���+���)');
INSERT INTO  bc_fields(name_,note_) VALUES ('INN_pl','��� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('KPP_pl','��� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NBankCountPl','���� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePl','������������ ����� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePl_T','������������ ����� �����������(���+�����)');
INSERT INTO  bc_fields(name_,note_) VALUES ('Town_Pl','����� ����� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('KS_BankPl','������� ����� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BIK_BankPl','��� ����� �����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol','������������ ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol_T','������������ ����������(���+�����)');
INSERT INTO  bc_fields(name_,note_) VALUES ('NamePol_INN','������������ ����������(���+���)');
INSERT INTO  bc_fields(name_,note_) VALUES ('INN_pol','��� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('KPP_pol','��� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('NBankCountPol','���� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePol','������������ ����� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BankNamePol_T','������������ ����� ����������(���+�����)');
INSERT INTO  bc_fields(name_,note_) VALUES ('Town_Pol','����� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('KS_BankPol','������� ����� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('BIK_BankPol','��� ����� ����������');
INSERT INTO  bc_fields(name_,note_) VALUES ('PaymentDetails','���������� �������');
INSERT INTO  bc_fields(name_,note_) VALUES ('TypeOper','��� ��������');
INSERT INTO  bc_fields(name_,note_) VALUES ('DocType','��� ���������');
INSERT INTO  bc_fields(name_,note_) VALUES ('SendType','��� ��������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Payment','�������� ���������');
INSERT INTO  bc_fields(name_,note_) VALUES ('PaymentType','��� �������');
INSERT INTO  bc_fields(name_,note_) VALUES ('Date_cd','���� �������� �����');
INSERT INTO  bc_fields(name_,note_) VALUES ('Time_cd','����� �������� �����');

--central_kernel: bc_schema
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,1,1 ,'Content-Type=doc/payment','��� ���������'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,1 ,'DATE_DOC='	 ,'���� ���������'		   ,5   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,2 ,'NUM_DOC='	 ,'����� ���������'		   ,6   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,3 ,'PAYMENT_TYPE='	 ,'��� �������'			   ,34  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,4 ,'PAYER_INN='	 ,'��� �����������'		   ,10  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,5 ,'PAYER_NAME='	 ,'������������ �����������'	   ,7   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,6 ,'PAYER_ACCOUNT='	 ,'���� �����������'		   ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,7 ,'AMOUNT='	 ,'����� �������'		   	   ,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,8 ,'PAYER_BANK_NAME=','������������ ����� �����������'  ,13  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,9 ,'PAYER_BANK_BIC=' ,'��� ����� �����������'	   ,17  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,10,'PAYER_BANK_ACC=' ,'������� ����� �����������'	   ,16  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,11,'RCPT_INN='	 ,'��� ����������'		   ,21  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,12,'RCPT_NAME='	 ,'������������ ����������'	   ,18  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,13,'RCPT_ACCOUNT='	 ,'���� ����������'		   ,23  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,14,'RCPT_BANK_NAME=' ,'������������ ����� ����������'   ,24  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,15,'RCPT_BANK_BIC='	 ,'��� ����� ����������'	   ,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,16,'RCPT_BANK_ACC='	 ,'������� ����� ����������'	   ,27  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,17,'TYPE_OPER='	 ,'��� ��������'		   ,30  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,18,'QUEUE='		 ,'����������� �������'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,19,'PAYMENT_DETAILS=','���������� �������'		   ,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,20,'KPP='		 ,'��� �����������'		   ,11  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,21,'TERM='		 ,'���� �������'		   ,2   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,22,'RCPT_KPP='	 ,'��� ����������'		   ,22  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,23,'IS_CHARGE='	 ,'������� ���������� �������'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,24,'CHARGE_CREATOR=' ,'������ ����������� ���������'    ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,25,'CHARGE_KBK='	 ,'��� ��������� �������������'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,26,'CHARGE_OKATO='	 ,'��� OKATO'			   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,27,'CHARGE_BASIS='	 ,'��������� �������'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,28,'CHARGE_PERIOD='	 ,'��������� ������'		   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,29,'CHARGE_NUM_DOC=' ,'������ - ����� ���������'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,30,'CHARGE_DATE_DOC=','������ - ���� ���������'	   ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (1,2,31,'CHARGE_TYPE='	 ,'��� �������'			   ,null,0,1);

INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,1 ,'1CClientBankExchange'	,'���������� ������� ����� ������'	  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,2 ,'�������������='	  	,'����� ������ ������� ������'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,3 ,'���������='	  	,'��������� �����'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,4 ,'�����������='		,'���������-�����������'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,5 ,'����������='		,'���������-����������'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,6 ,'������������='		,'���� ������������ �����'		  ,35  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,1,7 ,'�������������='		,'����� ������������ �����'		  ,36  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,1 ,'����������='		,'���� ������ ���������'		  ,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,2 ,'���������='		,'���� ����� ���������'			  ,2   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,3 ,'��������='		,'��������� ���� �����������'	  	  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,4 ,'��������='		,'������������ ���������'		  ,33  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,5 ,'��������������='	,'������� ������ ������'		  	  ,33  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,6 ,'�����='			,'����� ���������'			  ,6   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,7 ,'����='			,'���� ���������'			  ,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,8 ,'�����='			,'����� �������'			  ,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,9 ,'��������������='	,'��������� ���� �����������'		  	  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,10,'�������������='		,'��� �����������'			  ,10  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,11,'�������������='		,'��� �����������'			  ,11  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,12,'����������='		,'������������ �����������'		  ,9   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,13,'����������1='		,'������������ �����������'		  ,7   ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,14,'������������������='	,'��������� ���� �����������'		  ,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,15,'��������������1='	,'���� �����������'			  ,14  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,16,'��������������2='	,'���� �����������'			  ,15  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,17,'�������������='		,'��� ����� �����������'		  ,17  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,18,'�����������������='	,'������� ����� ����������'		  ,16  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,19,'��������������='	,'��������� ���� ����������'	 	  	  ,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,20,'�������������='		,'��� ����������'			  ,21  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,21,'�������������='		,'��� ����������'			  ,22  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,22,'����������='		,'������������ ����������'		  ,20  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,23,'����������1='		,'������������ ����������'		  ,18  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,24,'������������������='	,'��������� ���� ����������'		  ,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,25,'��������������1='	,'������������ ����� ����������' 	  ,25  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,26,'��������������2='	,'������������ ����� ����������'  	  ,26  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,27,'�������������='		,'��� ����� ����������'			  ,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,28,'�����������������='	,'������� ����� ����������'		  ,27  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,29,'����������='		,'��� �������'				  ,34  ,0,0);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,30,'���������='		,'��� ������ (��� ��������)'		  ,30  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,31,'�����������='		,'���� �������'				  ,2   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,32,'�����������='		,'����������� �������'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,33,'�����������������='	,'������ ����������� ���������� ���������',null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,34,'�����������������='	,'���������� �������'			  ,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,35,'�����������������1='	,'���������� �������' 			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,36,'�����������������2='	,'���������� �������'			  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,2,37,'��������������'		,'������� ��������� ������'		  ,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (2,3,1 ,'����������'		,'������� ����� �����'			  ,null,0,1);

INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,1 ,'%START '          ,'������ ���������'			,null,0,1);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,2 ,'Number         : ','����� ���������� ���������'		,6   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,3 ,'Debit          : ','��������� ���� �� ������'		,23  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,4 ,'Receiver       : ','������������ ����������'		,18  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,5 ,'Recvbank       : ','������������ ����� ����������'	,25  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,6 ,'Recvcode       : ','��� ��� ����� ����������'		,28  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,7 ,'Credit         : ','��������� ���� �� �������'		,12  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,8 ,'Recvkorr       : ','�������� ����������'			,27  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,9 ,'Recvinn        : ','��� ����������'			,21  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,10,'RecvKPP        : ','��� ����������'			,22  ,0,0   );
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,11,'Ammount        : ','����� �������'			,1   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,12,'Memo           : ','���������� �������'			,29  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,13,'Doctype        : ','��� ���������'			,31  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,14,'Sendtype       : ','��� ��������'			,32  ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,15,'Dateplat       : ','���� �������'			,5   ,1,null);
INSERT INTO  bc_schema(id_bc_type,id_bc_row_type,num,tag_name,tag_descr,id_bc_field,is_requared,is_show_empty) VALUES (3,2,16,'%STOP'            ,'��������� ���������'			,null,0,1   );




