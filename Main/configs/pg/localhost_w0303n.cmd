SET W1="BSlNAat4J7uYgiRv-u39waKOfLga0CqQCX1Va240SeXKNAs-nwfj5Z19t1yhaUlYJ8gwSoIDy2oY6fkiVmGIzOWilq8ezOBvvJOPJ3uxmTi3ZJmhSWs3GgecbpqMNnOLLOBf38h_ZleZUZmyBdrxeA8YtMov2PcSUyKhIJsTuLV33oZDLrc4vNP5O1sOggiilbuWhcnhPbEIaV-J_Coc_7GKu_P0Gxk2kLISyA0sqOEo7IDzcSS0AurA65OMIQmxALxQ0VXuk2Y2JKdFnChf6w=="
SET W2="FOW9KhF8RVDp9SkkR4SOvXvlsW_gmeAS03ZS22uNfplM7nVIC3BY_fqRrxh3LFvd"
SET W3="4KJn-RvR93YgFQelgwfwPRVU79rLu0XUfjFum6jywYPmCMopyOOECOZE1mdIj_PJeXsmHv5CfL51bu2TENyGmQ=="
SET W4="BSlNAat4J7uYgiRv-u39waKOfLga0CqQCX1Va240SeXKNAs-nwfj5Z19t1yhaUlYJ8gwSoIDy2oY6fkiVmGIzOWilq8ezOBvvJOPJ3uxmTi3ZJmhSWs3GgecbpqMNnOLLOBf38h_ZleZUZmyBdrxeA8YtMov2PcSUyKhIJsTuLV33oZDLrc4vNP5O1sOggiilbuWhcnhPbEIaV-J_Coc_7GKu_P0Gxk2kLISyA0sqOEo7IDzcSS0AurA65OMIQmxALxQ0VXuk2Y2JKdFnChf6w=="
SET W5="4KJn-RvR93YgFQelgwfwPRVU79rLu0XUfjFum6jywYPUQO3PEFJ1_aF2cab2_LuJ1pFZp8wr3_uT4Pufk7HipQ=="
SET W6="Hev6cNBlh046du1X4Rsr4OxN3zfXy_6TgpoRWQcMvrnYbpc3tjGO2i8D2q_ziy1VJYu9TneQdj-5YRKUHTY1msZB9olX5lOBxZelM2LKcE27Z7042Ei0nT1tmZhoF7h6lCHpYnzWRAk-nYaQoGQnzoukp3pg0T4OwMJJAKVOXU0="
SET W9="jBtOGL8ln86oOx57u4bnIw=="
SET W10="rE-TlzQ7bRQPDROQ9pYNUA=="
SET DbWaitingTimeout="O2mN6Gnl-dDIaFcmN2wiOg=="
SET FtpHostAddress="2Uv9GM2W0dEY4GYOTl5jE4_ulwWXkZs10hVUCiKuhog="
SET FtpUserName="FOW9KhF8RVDp9SkkR4SOvWirYzFA8_EYwU_PSWDARhM="
SET FtpUserPassword="u-cjEMZ6iZ-lqlwI9rghX6igqod4mlE7nY5qeiv95e8="
SET FtpUseProxy="37zkYqcoEXvvscFF41sNow=="

SET filename="Host.user.config"
del %filename%

echo ^<appSettings^> >> %filename%
echo ^<add key="W1" value=%W1% /^> >> %filename%
echo ^<add key="W2" value=%W2% /^> >> %filename%
echo ^<add key="W3" value=%W3% /^> >> %filename%
echo ^<add key="W4" value=%W4% /^> >> %filename%
echo ^<add key="W5" value=%W5% /^> >> %filename%
echo ^<add key="W6" value=%W6% /^> >> %filename%
echo ^<add key="W9" value=%W9% /^> >> %filename%
echo ^<add key="W10" value=%W10% /^> >> %filename%
echo ^<add key="DbWaitingTimeout" value=%DbWaitingTimeout% /^> >> %filename%
echo ^<add key="FtpHostAddress" value=%FtpHostAddress% /^> >> %filename%
echo ^<add key="FtpUserName" value=%FtpUserName% /^> >> %filename%
echo ^<add key="FtpUserPassword" value=%FtpUserPassword% /^> >> %filename%
echo ^<add key="FtpUseProxy" value=%FtpUseProxy% /^> >> %filename%
echo ^</appSettings^> >> %filename%

