// Версия 1.04 от 06.03.2018г. Загрузка в `Скрудж` выплат от пенсионного фонд.
using	MyTypes;
using	__ = MyTypes.CCommon;

public	class	PfuImporter {

	static	CCommand	Command					;
	static	int			TotalLines			=	0	;
	static	long		TotalCents			=	0	;
	static	string		BankCode			=	CAbc.EMPTY;
	static	string		DebitName			=	CAbc.EMPTY;
	static	string		DebitState			=	CAbc.EMPTY;
	static	string		DebitMoniker		=	CAbc.EMPTY;
	static	string		Purpose				=	CAbc.EMPTY;
	static	string		NewPurpose			=	CAbc.EMPTY;
	static	string		NewDebitMoniker		=	CAbc.EMPTY;
	static	readonly string	DEBIT_ALIAS		=	"AccountA_Text" ;
	static	readonly string	PURPOSE_ALIAS	=	"Argument_Text" ;
	static	readonly string	USER_NAME		=	__.Upper( __.GetUserName() ) ;
	static	string		ModelFileName		=	__.GetTempDir() + "\\" + "AMaker.mod" ;

	//  ----------------------------------------------------------------------------
	//  Функцию Main нужно пометить атрибутом [STAThread], чтоб работал OpenFileBox
	[System.STAThread]
	public	static void Main() {//FOLD01
		const	bool	DEBUG		=	false		;
		bool			UseErcRobot	=	false		;	// подключаться ли к серверу под логином ErcRobot
		const	string	ROBOT_LOGIN=	"ErcRobot"	;
		const	string	ROBOT_PWD	=	"35162987"	;
		const	string	TASK_CODE	=	"OpenGate"	;
		byte	SavedColor							;
		COpengateConfig	OgConfig					;
		string		ConnectionString=	CAbc.EMPTY
		,		CleanFileName	=	CAbc.EMPTY
		,		TmpFileName	=	CAbc.EMPTY
		,		LogFileName	=	CAbc.EMPTY
		,		ScroogeDir	=	CAbc.EMPTY
		,		SettingsDir	=	CAbc.EMPTY
		,		ServerName	=	CAbc.EMPTY
		,		DataBase	=	CAbc.EMPTY
		,		FileName	=	CAbc.EMPTY
		,		AboutError	=	CAbc.EMPTY
		,		InputDir	=	CAbc.EMPTY
		,		TodayDir	=	CAbc.EMPTY
		,		StatDir		=	CAbc.EMPTY
		,		TmpDir		=	CAbc.EMPTY
		;
		int		WorkDate	=	-1
		,		SeanceNum	=	-1
		,		UniqNum		=	0
		,		i			=	0
		;
		CConnection	Connection = null					;
		System.Console.BackgroundColor	=	0			;
		System.Console.Clear()						;
		Err.LogToConsole() ;
		CCommon.Print( "","  Загрузка в `Скрудж` выплат от пенс.фонда. Версия 1.04 от 06.03.2018г." );
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// считываем конфигурацию Скрудж-2
		CScrooge2Config	Scrooge2Config	= new	CScrooge2Config();
		if	(!Scrooge2Config.IsValid) {
			CCommon.Print( Scrooge2Config.ErrInfo ) ;
			return	;
		}
		ScroogeDir	=	(string)Scrooge2Config["Root"]		;
		SettingsDir	=	(string)Scrooge2Config["Common"]	;
		ServerName	=	(string)Scrooge2Config["Server"]	;
		DataBase	=	(string)Scrooge2Config["DataBase"]	;
		if	( ServerName == null ) {
			CCommon.Print("  Не найдена переменная `Server` в настройках `Скрудж-2` ");
			return;
		}
		if	( DataBase == null ) {
			CCommon.Print("  Не найдена переменная `Database` в настройках `Скрудж-2` ");
			return;
		}
		System.Console.Title="Загрузка в `Скрудж` выплат от пенс.фонда";
		__.DeleteOldTempDirs("??????" , __.Today() - 1 );
		if	( DEBUG )
			FileName ="D:\\WorkShop\\0000105.015";
		else
			if	( __.ParamCount() > 1 )
				for	( i= 1 ; i< __.ParamCount() ; i++ )
					if	( CAbc.ParamStr[ i ].Trim().ToUpper() == "/R" ) {
						UseErcRobot	= true;
						System.Console.Title = System.Console.Title + " * ";
					}
					else
						FileName	=	CAbc.ParamStr[ i ].Trim();
		if	( __.IsEmpty( FileName ) ) {
			__.Print( "  Формат запуска :   PfuImporter.exe [/R] <FileName> " );
			__.Print( "  Пример         :   PfuImporter.exe      * " );
			return;
		}
		if	(	( ! __.IsEmpty( ScroogeDir ) )
			&&	( ! __.IsEmpty( SettingsDir ) )
			)
			SettingsDir	=	ScroogeDir.Trim() + CAbc.SLASH + SettingsDir.Trim();
		else
			SettingsDir	=	CAbc.EMPTY;
		if	( FileName == "*" )
			FileName	=	SelectFileNameGUI( SettingsDir );
		if	( ! __.FileExists( FileName ) ) {
			__.Print( " Не найден указанный файл ! " );
			return;
		}
		CleanFileName	=	__.GetFileName( FileName );
		UniqNum		=	GetUniqNum( FileName );
		if	( __.GetExtension( CleanFileName ).Trim().ToUpper() == ".MOD" ) {
			if	( __.FileExists( ModelFileName ) )
				__.DeleteFile( ModelFileName ) ;
			__.CopyFile( FileName , ModelFileName );
			return;
		}
		if	( ! __.IsDigit( CleanFileName.Replace(".","") ) ) {
			__.Print( " Указан неправильный файл ! " );
			return;
		}
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// подключаемся к SQL-серверу
		ConnectionString	=	"Server="	+	ServerName
					+	";Database="	+	DataBase	;
		if	( UseErcRobot )
				ConnectionString	+=	";UID=" + ROBOT_LOGIN + ";PWD=" + ROBOT_PWD + ";" ;
		else
				ConnectionString	+=	";Integrated Security=TRUE;" ;
		try {
			Connection		= new	CConnection( ConnectionString );
		} catch ( System.Exception Excpt ) {
			CCommon.Print( Excpt.ToString() );
			CCommon.Print( "","  Ошибка подключения к серверу !" );
		}
		if	( ! Connection.IsOpen() ) {
			CCommon.Print( "","  Ошибка подключения к серверу !" );
			return;
		}
		else
			if	( DEBUG )
				CCommon.Print("  Сервер        :  " + ServerName );
		Command			= new	CCommand(Connection) ;
		if	( ! Command.IsOpen() )  {
			CCommon.Print( "  Ошибка подключения к базе данных !" );
			return;
		}
		else
			if	( DEBUG )
				CCommon.Print("  База данных   :  " + DataBase );
		System.Console.Title=System.Console.Title+"   | "+ServerName+"."+DataBase	;
		CConsole.Clear();
		LoadModel( ModelFileName ) ;
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// считываем настройки универсального шлюза
		WorkDate		=	( int ) __.IsNull( Command.GetScalar( " exec  dbo.pMega_OpenGate_Days;7 " ) , (int) 0 );
		if	( WorkDate < 1 ) {
			__.Print( " Ошибка определения даты текущего рабочего дня. " );
			Command.Close();
			Connection.Close();
			return	;
		}
		OgConfig	= new	COpengateConfig();
		OgConfig.Open( WorkDate );
		if	( ! OgConfig.IsValid() ) {
			__.Print( "  Ошибка чтения настроек программы из " + OgConfig.Config_FileName() );
			__.Print( OgConfig.ErrInfo())		;
			Command.Close();
			Connection.Close();
			return;
		}
		SeanceNum	=	( int ) __.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;4  @TaskCode='" + TASK_CODE + "',@ParamCode='NumSeance' ") , (int) 0 );
		if	( SeanceNum < 1 ) {
			__.Print( " Ошибка определения номера сеанса " );
			Command.Close();
			Connection.Close();
			return	;
		}
		TodayDir	=	(string)OgConfig.TodayDir()		;
		TmpDir		=	(string)OgConfig.TmpDir()		;
		StatDir		=	(string)OgConfig.StatDir()		;
		if ( (TodayDir == null) || (InputDir == null) ) {
			__.Print( "  Ошибка чтения настроек программы из " + OgConfig.Config_FileName() );
			Command.Close();
			Connection.Close();
			return;
		}
		TodayDir	=	TodayDir.Trim() ;
		StatDir		=	StatDir.Trim();
		TmpDir		=	TmpDir.Trim();
		if	( ! __.DirExists( TodayDir ) )
			__.MkDir( TodayDir );
		if	( ! __.DirExists( StatDir ) )
			__.MkDir( StatDir );
		if	( ! __.DirExists( TmpDir ) )
			__.MkDir( TmpDir );
		if	( ! __.SaveText( StatDir + "\\" + "test.dat" , "test.dat" , CAbc.CHARSET_DOS ) ) {
			__.Print( " Ошибка записи в каталог " + StatDir );
			Command.Close();
			Connection.Close();
			return	;
		}
		__.DeleteFile(StatDir + "\\" + "test.dat");
		LogFileName	=	OgConfig.LogDir() + "\\SEANS" + SeanceNum.ToString("000")  + ".TXT";
		Err.LogTo( LogFileName );
		__.AppendText( LogFileName , __.Now() + "   " + USER_NAME + "  загружает файл " + CleanFileName + CAbc.CRLF , CAbc.CHARSET_WINDOWS );
		TmpFileName		=	TodayDir + CAbc.SLASH + CleanFileName	;
		if	( ! __.FileExists( TmpFileName ) )
			__.CopyFile( FileName , TmpFileName ) ;
		if	( ! __.FileExists( TmpFileName ) ) {
			__.Print( " Ошибка записи файла " + TmpFileName );
			Command.Close();
			Connection.Close();
			return ;
		}
		TmpFileName		=	TmpDir + CAbc.SLASH
					+	__.Right( "0" + __.Hour(__.Clock()).ToString() , 2 )
					+	__.Right( "0" + __.Minute( __.Clock()).ToString() , 2 )
					+	__.Right( "0" + __.Second( __.Clock()).ToString() , 2 )	;
		if( ! __.DirExists( TmpFileName ) )
			__.MkDir( TmpFileName )	;
		TmpFileName		=	TmpFileName + CAbc.SLASH + CleanFileName	;
		if	( __.FileExists( TmpFileName ) )
			__.DeleteFile( TmpFileName )	;
		if	( __.FileExists( TmpFileName ) ) {
			__.Print("Ошибка удаления файла ",TmpFileName)	;
			Command.Close();
			Connection.Close();
			return	;
		}
		__.CopyFile( FileName , TmpFileName )	;
		if	( DEBUG )
			__.Print("  Беру настройки шлюза здесь :  " + OgConfig.Config_FileName() );
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// проверяем пачку
		AboutError	=	CheckPfuFile( FileName , UniqNum ) ;
		if	( __.IsEmpty( AboutError ) )  {
			if	( ! CConsole.GetBoxChoice(
								" Всего строк : " + TotalLines.ToString()
							,	" Общая сумма : " + ( __.CCur( TotalCents ) / 100 ).ToString().Replace(",",".")
							,	" Дебет.счет  : " + __.Left(DebitMoniker , 26 )
							,	__.Left( DebitName  , 42 )
							,	"__________________________________________"
							,	" Для загрузки нажмите ENTER . "
							,	" Для выхода - ESC."
							)
				) {
				__.AppendText( LogFileName ,  CAbc.CRLF + __.Now() + "  загрузка отменена. " + CAbc.CRLF , CAbc.CHARSET_WINDOWS );
				Command.Close();
				Connection.Close();
				return;
			}
		}
		else	{
			__.Print( AboutError );
			__.AppendText( LogFileName ,  CAbc.CRLF + AboutError + CAbc.CRLF , CAbc.CHARSET_WINDOWS );
			SavedColor		=	CConsole.BoxColor ;
			CConsole.BoxColor	=	CConsole.RED*16 + CConsole.WHITE	;
			if	( ! CConsole.GetBoxChoice(	" При проверке файла обнаружены ошибки !"
							,	"__________________________________________"
							,	" Всего строк : " + TotalLines.ToString()
							,	" Общая сумма : " + ( __.CCur( TotalCents ) / 100 ).ToString().Replace(",",".")
							,	" Дебет.счет  : " + __.Left(DebitMoniker , 26 )
							,	__.Left( DebitName  , 42 )
							,	"__________________________________________"
							,	" Для отмены загрузки нажмите ESC . "
							,	" Для выполнения загрузки - ENTER ."
							)
				) {
				__.AppendText( LogFileName ,  CAbc.CRLF + __.Now() + "  загрузка отменена. " + CAbc.CRLF , CAbc.CHARSET_WINDOWS );
				CConsole.BoxColor	=	SavedColor ;
				Command.Close();
				Connection.Close();
				return;
			}
			CConsole.BoxColor	=	SavedColor ;
		}
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		// загружаем пачку
		LoadPfuFile( FileName , UniqNum );
		__.AppendText( LogFileName ,  CAbc.CRLF + __.Now() + "  загрузка завершена. " + CAbc.CRLF , CAbc.CHARSET_WINDOWS );
		CConsole.ShowBox(CAbc.EMPTY," Подождите..." ,CAbc.EMPTY) ;
		Command.Execute("  exec pMega_OpenGate_PayRoll;2 "
			+	"  @FileName='"  + CleanFileName + "-" + UniqNum.ToString().Trim() + "'"
			+	", @DayDate=" + WorkDate.ToString()
			) ;
		CConsole.Clear();
		Connection.Close();
	}//FOLD01

	//  ----------------------------------
	// Загрузка всех строк входного файла
	static	bool	LoadPfuFile( string  FileName , int UniqNum ) {//fold01
		if	( Command == null )
			return	false;
		if	( FileName == null )
			return	false;
		bool		Result		=	true;
		string		CmdText		=	CAbc.EMPTY;
		string		ShortFileName		=	__.GetFileName( FileName ).Trim() + "-" + UniqNum.ToString().Trim();
		CPfuReader	PfuReader	= new	CPfuReader();
		int		LineNum		=	0 ;
		if	( PfuReader.Open( FileName  ) )
			while	( PfuReader.Read() ) {
				LineNum		++	;
				CConsole.ShowBox(""," Загружается строка" + CCommon.StrI( LineNum , 5 ) + " " ,"") ;
				CmdText		=	"exec  dbo.pMega_OpenGate_AddPalvis "
						+	" @TaskCode     = 'OpenGate'"
						+	",@BranchCode   = ''"
						+	",@USerName		= '"	+	USER_NAME + "'"
						+	",@FileName     = '"	+	ShortFileName + "'"
						+	",@LineNum      =  "	+	LineNum.ToString()
						+	",@Code         = '"	+	( LineNum + UniqNum ).ToString().Trim()+ "'"
						+	",@Ctrls        = ''"
						+	",@SourceCode   = '"	+	BankCode	+	"'"
						+	",@DebitMoniker = '"	+	DebitMoniker	+	"'"
						+	",@DebitName    = '"	+	DebitName.Replace("'","`")	+	"'"
						+	",@DebitState   = '"	+	DebitState	+	"'"
						+	",@TargetCode   = '"	+	BankCode	+	"'"
						+	",@CreditMoniker= '"	+	PfuReader.AccountNum() + "'"
						+	",@CreditName   = '"	+	PfuReader.ClientName().Replace("'","`") + "'"
						+	",@CreditState  = '"	+	PfuReader.IdentCode() + "'"
						+	",@CrncyAmount  =  "	+	PfuReader.Cents().ToString()
						+	",@CurrencyId   =  980 "
						+	",@DayDate	=  "	+	__.Today().ToString()
						+	",@OrgDate	=  "	+	__.Today().ToString()
						+	",@Purpose      = '"	+	Purpose.Replace("'","`") + "'"
						;
				if	( ! Command.Execute( CmdText ) )
					Result	=	false;
			}
		else
			Result	=	false;
		CConsole.Clear();
		PfuReader.Close();
		return	Result;
	}//FOLD01

	//  -----------------------------------
	//  Проверка всех строк входного файла
	static	string	CheckPfuFile( string FileName , int UniqNum ) {//fold01
		if	( Command == null )
			return	"Ошибка подключения к серверу !";
		if	( FileName == null )
			return	"Ошибка определения имени файла !";
		string	Result		=	CAbc.EMPTY
		,	CmdText		=	CAbc.EMPTY
		,	AboutError	=	CAbc.EMPTY	;
		bool	HaveError	=	false		;
		string	ShortFileName		=	__.GetFileName( FileName ).Trim() + "-" + UniqNum.ToString().Trim();
		CPfuReader	PfuReader	= new	CPfuReader();
		int	LineNum		=	0;
		TotalCents		=	0;
		if	( PfuReader.Open( FileName  ) )
			while	( PfuReader.Read() ) {
				LineNum		++	;
				TotalCents	+=	PfuReader.Cents() ;
				CConsole.ShowBox(CAbc.EMPTY," Проверяется строка" + __.StrI( LineNum , 5 ) + " " ,CAbc.EMPTY)	;
				CmdText		=	"exec  dbo.pMega_OpenGate_CheckPalvis "
						+	"   @Code         = '" + ( LineNum + UniqNum ).ToString().Trim() + "'"
						+	" , @Ctrls        = ''"
						+	" , @SourceCode   = '" + BankCode	+ "'"
						+	" , @DebitMoniker = '" + DebitMoniker + "'"
						+	" , @DebitState   = '" + DebitState  + "'"
						+	" , @TargetCode   = '" + BankCode	+ "'"
						+	" , @CreditMoniker= '" + PfuReader.AccountNum() + "'"
						+	" , @CreditState  = '" + PfuReader.IdentCode()  + "'"
						+	" , @CrncyAmount  =  " + PfuReader.Cents().ToString()
						+	" , @CurrencyId   =  980 "
						+	" , @UserName     = '" + USER_NAME + "'"
						;
           			AboutError	=	(string) __.IsNull( Command.GetScalar( CmdText ) , CAbc.EMPTY ) ;
				if	( __.IsEmpty( Purpose ) )
					AboutError	+=	" Не заполнено назначение платежа ;" ;
				if	( __.IsEmpty( DebitName ) )
					AboutError	+=	" Не заполнено название дб. счета ;" ;
				if	( __.IsEmpty( PfuReader.ClientName() ) )
					AboutError	+=	" Не заполнено название кт. счета ;" ;
				if	( AboutError != null )
					if	( ( AboutError.Trim() != "" ) ) {
							HaveError	=	true;
							Result		+=	" Ошибка в строке " + LineNum.ToString() +" : " + AboutError.Trim()  + CAbc.CRLF  ;
					}
			}
		else	{
			PfuReader.Close();
			return	"ошибка открытия файла " + ShortFileName ;
		}
		TotalLines	=	LineNum;
		CConsole.Clear();
		byte	SavedColor		=	CConsole.BoxColor;
		if	( ( ( int ) CCommon.IsNull( Command.GetScalar( "exec dbo.pMega_OpenGate_CheckPalvis;2 @TaskCode='OpenGate',@FileName='" + ShortFileName + "'" ) , (int) 0 ) ) > 0 ) {
			CConsole.BoxColor	=	CConsole.RED*16 + CConsole.WHITE	;
			CConsole.GetBoxChoice( "Файл " + ShortFileName + " сегодня уже загружался !" , "" ,"Нажмите ESC для выхода.") ;
			CConsole.BoxColor	=	SavedColor	;
			CConsole.Clear();
			return	"Файл " + ShortFileName + " сегодня уже загружался !" ;
		}
		return	Result;
	}//FOLD01

	//  -------------------------------------------------------
	// Получить информацию о дебетб-счете и назначении платежа
	static	void	LoadModel( string FileName ) {//fold01
                if	( FileName == null )	{
			__.Print("Ошибка : не указан файл с шаблоном !");
			return;
		}
                if	( __.IsEmpty( FileName ) == null )	{
			__.Print("Ошибка : не указан файл с шаблоном !");
			return;
		}
                if	( __.FileExists( FileName ) )	{
			CCfgFile        ModelFile      = new   CCfgFile( FileName ) ;
			DebitMoniker	=	(string) ModelFile[ DEBIT_ALIAS ];
			if	( DebitMoniker == null )
				DebitMoniker	=	CAbc.EMPTY;
			Purpose	=	(string) ModelFile[ PURPOSE_ALIAS ];
			if	( Purpose == null )
				Purpose		=	CAbc.EMPTY;
		}
		do {
			__.Write(	"Введите номер дебет.счета" );
			if	( __.IsEmpty( DebitMoniker ) )
				__.Write( " : " );
			else
				__.Write( " ( " + DebitMoniker + " )" + " : " );
			NewDebitMoniker	=	__.Input();
			if	( ! __.IsEmpty( NewDebitMoniker ) )
				DebitMoniker	=	NewDebitMoniker ;
			if	( ! __.IsEmpty( DebitMoniker ) )
				DebitName	=	(string) Command.GetScalar( "exec dbo.pMega_OpenGate_PayRoll @Mode=2 , @Moniker='" + DebitMoniker + "'" );
			else
				DebitName	=	CAbc.EMPTY;
			if	( DebitName == null )
				DebitName	=	CAbc.EMPTY;
			if	(	( __.IsEmpty( DebitName )  )
				||	( DebitName.Length < 33 )
				)
					DebitMoniker	=	CAbc.EMPTY;
				else	{
					BankCode	=	DebitName.Substring(0,16).Trim();
					DebitState	=	DebitName.Substring(16,16).Trim();
					DebitName	=	__.FixUkrI( DebitName.Substring(32).Trim() );
				}
		} while	( __.IsEmpty( DebitMoniker ) ) ;
		do {
			__.Write(	"Введите назначение платежа" ) ;
			if	( __.IsEmpty( Purpose ) )
				__.Write( " : " );
			else
				__.Write( " ( " + Purpose + " )" + " : " );
			NewPurpose	=	__.Input();
			if	( ! __.IsEmpty( NewPurpose ) )
				Purpose	=	NewPurpose;
		} while	( __.IsEmpty( Purpose )  ) ;
		if	(	( ! __.IsEmpty( NewPurpose ) )
			||	( ! __.IsEmpty( NewDebitMoniker  ) )
			)
			__.SaveText(	FileName
				,	DEBIT_ALIAS + CAbc.TAB + CAbc.TAB +  DebitMoniker + CAbc.CRLF
				+	PURPOSE_ALIAS + CAbc.TAB + CAbc.TAB + Purpose + CAbc.CRLF
				,	CAbc.CHARSET_WINDOWS
			) ;
	}//FOLD01

	//  ---------------------------------------------------------------
	//  Получить имя файла с помощью графической панели открытия файла
	static string SelectFileNameGUI( string SettingsPath ) {//fold01
		string		Result		=	CAbc.EMPTY;
		string		SettingsFileName=	null;
		if	( SettingsPath != null )
			if	( SettingsPath.Trim().Length > 0 ) {
		      		SettingsFileName	=	SettingsPath.Trim() + "\\" + CCommon.GetUserName() + ".ldr";
		      		if	( CCommon.FileExists( SettingsFileName ) )
					Result		=	CCommon.LoadText(  SettingsFileName , CAbc.CHARSET_WINDOWS );
				if	( Result == null )
				        Result	=	CAbc.EMPTY;
			}
		Result	=	Result.Trim();
		Result	=	__.OpenFileBox(
					"Выберите файл для обработки"
				,	Result
				,	"ведомости пенс.фонда|0*.0*"
			);
		if	( Result == null )
			return	CAbc.EMPTY;
		Result		=	Result.Trim();
                if	( __.IsEmpty( Result ) )
			return	Result;
		if	( SettingsFileName != null )
			CCommon.SaveText( SettingsFileName , __.GetDirName( Result ) , CAbc.CHARSET_WINDOWS ) ;
		return	Result;
	}//FOLD01

	//  -------------------------------------------------------------
	//  Получить уникальное число на основе содержимого пути к файлу
	public	static	int	GetUniqNum( string FileName ) {//fold01
		int	Result	=	0
		,	Mask	=	0xFFFF	;
		string	DirName	=	CCommon.GetCurDir();
		if	( FileName != null )
			if	( CCommon.GetDirName( FileName ).Length > 1 )
					DirName		=	CCommon.GetDirName( FileName );
		for	( int I = 0 ; I < DirName.Length ; I++ )
			Result	=	( Result * 5 + CCommon.Ord( DirName[ I ] ) ) & Mask;
		return	Result;
	}//FOLD01
}

//  -----------------------------------------------------------
//  Вычитка конфигурации универсального шлюза
public	class	COpengateConfig	:	CErcConfig//fold01
{
	public	override string StatDir()
	{
		return TodayDir() + "\\STA\\";
	}
        public	override string Config_FileName()
        {
        	return	"EXE\\GLOBAL.FIL";
        }
	public	override string	TodayDir()
	{
		string TmpS = __.DtoC(Erc_Date);
		return CfgFile["DaysDir"] + "\\" + TmpS.Substring(2, 6) + "\\";
	}
}//FOLD01

//  -----------------------------------------------------------
//  Вычитка полей из файла пенсионного фонда
public	class	CPfuReader {//fold01
	/*
			Структура информационной строки файла :
	1	Номер счета			N19	1-19
	2	Номер филиала			N5	20-24
	3	Код вклада			N3	25-27
	4	Сума (в коп.)			N19	28-46
	5	ФИО				C100	47-146
	6	Идентификационный номер		C10	147-156
	7	День выплаты			C2(/C3)	157-158(/159)
	8	CRLF				C2	159(/160)-160(/161)
	*/
	const	int	MIN_DATALINE_LENGTH  =	156 ;	// минимально-допустимая длина строки данных
	string		Buffer_of_the_header =	CAbc.EMPTY;
	string		Buffer_of_the_record =	CAbc.EMPTY;
	CTextReader	TextReader	= new	CTextReader();

	bool	Is_DataLine_Valid () {
		if	( Buffer_of_the_record	== null )
			return	false ;
		if	( Buffer_of_the_record.Length < MIN_DATALINE_LENGTH )
			return	false ;
		return	true;
	}

	public	void	Close() {
		Buffer_of_the_header	=	CAbc.EMPTY;
		Buffer_of_the_record	=	CAbc.EMPTY;
		TextReader.Close();
	}

	public	bool	Open( string FileName ) {
		Close();
		if	( FileName == null )
			return	false;
		if	( ! TextReader.Open( FileName , CAbc.CHARSET_DOS ) )
			return	false;
		if	( ! TextReader.Read() )
			return	false;
		Buffer_of_the_header	=	TextReader.Value;
		return	true;
	}

	public	bool	Read() {
		Buffer_of_the_record	=	CAbc.EMPTY;
		do {
			if	( ! TextReader.Read() )
				return	false ;
			if	( TextReader.Value == null )
				return	false ;
		} while	( TextReader.Value.Length < MIN_DATALINE_LENGTH );
		Buffer_of_the_record	=	TextReader.Value ;
		return	true;
	}

	public	long	Cents() {
		if	( ! Is_DataLine_Valid() )
			return	0;
		return	CCommon.CLng( Buffer_of_the_record.Substring(27,19).Trim() );
	}

	public	string	AccountNum() {
		if	( ! Is_DataLine_Valid() )
			return	CAbc.EMPTY;
		return	Buffer_of_the_record.Substring(0,19).Trim().TrimStart('0');
	}

	public	string	ClientName() {
		if	( ! Is_DataLine_Valid() )
			return	CAbc.EMPTY;
		return	Buffer_of_the_record.Substring(46,100).Trim();
	}

	public	string	IdentCode() {
		if	( ! Is_DataLine_Valid() )
			return	CAbc.EMPTY;
		return	Buffer_of_the_record.Substring(146,10).Trim();
	}

}//FOLD01