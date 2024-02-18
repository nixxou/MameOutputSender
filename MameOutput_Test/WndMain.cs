using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection.Metadata;

namespace MameOutput_Test
{
  public partial class WndMain : Form
  {
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		const int SW_HIDE = 0;
		#region Interface API WIn32

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern uint RegisterWindowMessage(string lpString);

	[return: MarshalAs(UnmanagedType.Bool)]
	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

	/// <summary>
	/// Allocation d'un pointer d'une structure quelconque structure
	/// </summary>
	public static IntPtr IntPtrAlloc<T>(T param)
	{
	  IntPtr retval = Marshal.AllocHGlobal(Marshal.SizeOf(param));
	  Marshal.StructureToPtr(param, retval, false);
	  return retval;
	}

	/// <summary>
	/// Désallocation d'un pointeur
	/// </summary>
	/// <param name="preAllocated">Pointer to a previously allocated memory</param>
	public static void IntPtrFree(ref IntPtr preAllocated)
	{
	  if (IntPtr.Zero == preAllocated)
		MessageBox.Show("MameHookerHelper->SendIdString() error : Impossible to free unallocated Pointer");
	  Marshal.FreeHGlobal(preAllocated);
	  preAllocated = IntPtr.Zero;
	}

	#endregion

	// Liste des messages personalisés utilisés par MAME pour l'échange de données inter-processus
	private const String MAME_START_STRING = "MAMEOutputStart";
	private const String MAME_STOP_STRING = "MAMEOutputStop";
	private const String MAME_UPDATE_STRING = "MAMEOutputUpdateState";
	private const String MAME_REGISTER_STRING = "MAMEOutputRegister";
	private const String MAME_UNREGISTER_STRING = "MAMEOutputUnregister";
	private const String MAME_GETID_STRING = "MAMEOutputGetIDString";

	//Mame inner data
	private readonly String MAME_ORIENTATION_STRING = @"Orientation(\\.\DISPLAY1)";
	private readonly String MAME_PAUSE_STRING = "pause";
	private const UInt32 MAME_ORIENTATION_ID = 12345;
	private const UInt32 MAME_STRING_ID = 12346;

	// Valeurs associées qui seront définies par les appels à l'API Win32 RegisterwindowMessage
	private uint _Mame_OnStartMsg = 0;
	private uint _Mame_OnStopMsg = 0;
	private uint _Mame_UpdateStateMsg = 0;
	private uint _Mame_RegisterClientMsg = 0;
	private uint _Mame_UnregisterClientMsg = 0;
	private uint _Mame_GetIdStringMsg = 0;

	//Handle de la fenêtre
	private IntPtr _hWnd = IntPtr.Zero;

	//Liste des clients connectés (client = MameHooker par exempleà
	private List<OutputClient> _RegisteredClients;

	//Liste des Outputs 
	private List<GameOutput> _Outputs;
	//BAckup des outputs pour différencier si il y a eu changement
	private List<GameOutput> _OutputsBefore;

	//Flag pour permettre l'envoi des Outputs à l'initioalisation
	private bool _FirstOutputs = true;

	//Définition du la macro HWND_BROADCAST (normalement défini dans le système Windows en c++ natif)
	private IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
	//Définition du message WM_COPYDATA (normalement défini dans le système Windows en c++ natif)
	private const UInt32 WM_COPYDATA = 0x004A;

	private const uint COPYDATA_MESSAGE_ID_STRING = 1;

	private static bool shouldStopReceiver = false;
	private static bool WaitForConnection = false;
	private static bool WaitForConnectionGunA = false;
	private static bool WaitForConnectionGunB = false;
	private static bool WaitForConnectionGunC = false;
	private static bool WaitForConnectionGunD = false;

		private Dictionary<string, int> OutputsList = new Dictionary<string, int>();
	private string GameName = "";
	Thread receiverThreadControl;
	Thread receiverThreadGunA;
	Thread receiverThreadGunB;
	Thread receiverThreadGunC;
	Thread receiverThreadGunD;

		private static NotifyIcon notifyIcon;
	private static ContextMenuStrip contextMenu;

		public WndMain(string gameName, List<string> outputsList)
	{
	  InitializeComponent();

			this.WindowState = FormWindowState.Minimized;
			this.Opacity = 0;
			this.ShowInTaskbar = false;

			GameName = gameName;
			_hWnd = this.Handle;

		// Créer le menu contextuel
		contextMenu = new ContextMenuStrip();
		ToolStripMenuItem closeMenuItem = new ToolStripMenuItem("Close");
		closeMenuItem.Click += CloseMenuItem_Click;
		contextMenu.Items.Add(closeMenuItem);

		// Créer et afficher l'icône dans la barre des tâches
		notifyIcon = new NotifyIcon();
		notifyIcon.Icon = SystemIcons.Information;
		notifyIcon.ContextMenuStrip = contextMenu;
		notifyIcon.Visible = true;
		notifyIcon.Text = "MameHookerProxy";




			//Initialisation de la liste des clients
			_RegisteredClients = new List<OutputClient>();

	  //Création de la liste des Outputs (Nom, ID)
	  _Outputs = new List<GameOutput>();
	  uint i = 0;
	  foreach (var output in outputsList)
	  {
		i++;
		_Outputs.Add(new GameOutput(output, i));
		OutputsList.Add(output, (int)i);
	  }

	  //1ere étape : enregistrement des variables des messages systèmes personalisés
	  _Mame_OnStartMsg = RegisterMameOutputMessage(MAME_START_STRING);
	  _Mame_OnStopMsg = RegisterMameOutputMessage(MAME_STOP_STRING);
	  _Mame_UpdateStateMsg = RegisterMameOutputMessage(MAME_UPDATE_STRING);
	  _Mame_RegisterClientMsg = RegisterMameOutputMessage(MAME_REGISTER_STRING);
	  _Mame_UnregisterClientMsg = RegisterMameOutputMessage(MAME_UNREGISTER_STRING);
	  _Mame_GetIdStringMsg = RegisterMameOutputMessage(MAME_GETID_STRING);

	  receiverThreadControl = new Thread(RunReceiverControl);
	  receiverThreadControl.IsBackground = true;
	  receiverThreadControl.Start();

	  receiverThreadGunA = new Thread(RunReceiverGunA);
	  receiverThreadGunA.IsBackground = true;
	  receiverThreadGunA.Start();

	  receiverThreadGunB = new Thread(RunReceiverGunB);
	  receiverThreadGunB.IsBackground = true;
	  receiverThreadGunB.Start();

		receiverThreadGunC = new Thread(RunReceiverGunC);
		receiverThreadGunC.IsBackground = true;
		receiverThreadGunC.Start();

		receiverThreadGunD = new Thread(RunReceiverGunD);
		receiverThreadGunD.IsBackground = true;
		receiverThreadGunD.Start();

			Start();
	  timer1.Enabled = true;

	}

	private void CloseMenuItem_Click(object sender, EventArgs e)
  {
			
		shouldStopReceiver = true;
		if (WaitForConnection)
		{
			NamedPipeClientStream clt = new NamedPipeClientStream(".", "MameHookerProxyControl", PipeDirection.Out);
			clt.Connect();
			clt.Close();
		}
		receiverThreadControl.Join();
		QuitApp();
	}

  private static void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
  {
      // Afficher le menu contextuel lors d'un clic droit
      if (e.Button == MouseButtons.Right)
      {
          contextMenu.Show(Cursor.Position);
      }
  }

	public void RunReceiverControl()
	{
	  while (shouldStopReceiver == false)
	  {
		try
		{
		  using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MameHookerProxyControl", PipeDirection.In))
		  {
			WaitForConnection = true;
			pipeServer.WaitForConnection();
			WaitForConnection = false;
			Console.WriteLine("Connected to the command-line application.");

			using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
			{
			  char[] buffer = new char[4096];
			  while (pipeServer.IsConnected)
			  {
				if (shouldStopReceiver) return;

				int bytesRead = reader.Read(buffer, 0, buffer.Length);

				if (bytesRead == 0)
				  break; // Fin du flux, sortir de la boucle

				string message = new string(buffer, 0, bytesRead);

				foreach (var lines in message.Split('\n'))
				{
				  //Console.WriteLine($"Control : " + lines);
				  if (lines == "CLOSE")
				  {
					Task.Run(() =>
					{
					  this.Invoke(new Action(() =>
					  {
							this.QuitApp();
						}));
					});
					return;
					break;
				  }
				  if (lines != "" && lines.Contains(":"))
				  {
					var splitLine = lines.Split(':');
					if (splitLine.Length == 2 && !string.IsNullOrEmpty(splitLine[1]) && OutputsList.ContainsKey(splitLine[0]))
					{
					  int value = 0;
					  if (int.TryParse(splitLine[1], out value))
					  {
						if (value >= 0)
						{
						  _Outputs[OutputsList[splitLine[0]]].OutputValue = value;
						  this.SendValues(_Outputs);
						}
					  }
					}
				  }
				}

			  }
			}
		  }
		}
		catch (IOException ex)
		{
		  Console.WriteLine($"Error: {ex.Message}");
		}

		Thread.Sleep(15); // Sleep for a while before checking for new connections/messages.
	  }
	}

	public void RunReceiverGunA()
	{

	  while (shouldStopReceiver == false)
	  {
		try
		{
		  using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MameHookerProxyRecoilGunA", PipeDirection.In))
		  {
			WaitForConnectionGunA = true;
			pipeServer.WaitForConnection();
			WaitForConnectionGunA = false;
			using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
			{
			  char[] buffer = new char[4096];
			  while (true)
			  {
				if (shouldStopReceiver) return;

				int bytesRead = reader.Read(buffer, 0, buffer.Length);

				if (bytesRead == 0)
				  break; // Fin du flux, sortir de la boucle

				string message = new string(buffer, 0, bytesRead);
				long timestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				int gunshot = 0;
				foreach (var lines in message.Split('\n'))
				{
				  if (lines == "1") gunshot = 1;
				  //Console.WriteLine("GUN A : " + lines);
				}
				if (_Outputs[1].OutputValue != gunshot)
				{
				  _Outputs[1].OutputValue = gunshot;
				  this.SendValues(_Outputs);
				  if (gunshot == 1)
				  {
					Thread.Sleep(25);
					_Outputs[1].OutputValue = 0;
					this.SendValues(_Outputs);
				  }
				}
			  }
			}
		  }
		}
		catch (IOException ex)
		{
		  Console.WriteLine($"Error: {ex.Message}");
		}

		Thread.Sleep(30); // Sleep for a while before checking for new connections/messages.
	  }
	}

	public void RunReceiverGunB()
	{

	  while (shouldStopReceiver == false)
	  {
		try
		{
		  using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MameHookerProxyRecoilGunB", PipeDirection.In))
		  {
			WaitForConnectionGunB = true;
			pipeServer.WaitForConnection();
			WaitForConnectionGunB = false;
			using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
			{
			  char[] buffer = new char[4096];
			  while (true)
			  {
				if (shouldStopReceiver) return;

				int bytesRead = reader.Read(buffer, 0, buffer.Length);

				if (bytesRead == 0)
				  break; // Fin du flux, sortir de la boucle

				string message = new string(buffer, 0, bytesRead);
				long timestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

				int gunshot = 0;
				foreach (var lines in message.Split('\n'))
				{
				  if (lines == "1") gunshot = 1;
				  //Console.WriteLine("GUN B : " + lines);
				}
				if (_Outputs[2].OutputValue != gunshot)
				{
				  _Outputs[2].OutputValue = gunshot;
				  this.SendValues(_Outputs);
				  if (gunshot == 1)
				  {
					Thread.Sleep(25);
					_Outputs[2].OutputValue = 0;
					this.SendValues(_Outputs);
				  }

				}
			  }
			}
		  }
		}
		catch (IOException ex)
		{
		  Console.WriteLine($"Error: {ex.Message}");
		}

		Thread.Sleep(15); // Sleep for a while before checking for new connections/messages.
	  }
	}

	public void RunReceiverGunC()
	{

		while (shouldStopReceiver == false)
		{
			try
			{
				using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MameHookerProxyRecoilGunC", PipeDirection.In))
				{
					WaitForConnectionGunC = true;
					pipeServer.WaitForConnection();
					WaitForConnectionGunC = false;
					using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
					{
						char[] buffer = new char[4096];
						while (true)
						{
							if (shouldStopReceiver) return;

							int bytesRead = reader.Read(buffer, 0, buffer.Length);

							if (bytesRead == 0)
								break; // Fin du flux, sortir de la boucle

							string message = new string(buffer, 0, bytesRead);
							long timestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

							int gunshot = 0;
							foreach (var lines in message.Split('\n'))
							{
								if (lines == "1") gunshot = 1;
								//Console.WriteLine("GUN C : " + lines);
							}
							if (_Outputs[3].OutputValue != gunshot)
							{
								_Outputs[3].OutputValue = gunshot;
								this.SendValues(_Outputs);
								if (gunshot == 1)
								{
									Thread.Sleep(25);
									_Outputs[3].OutputValue = 0;
									this.SendValues(_Outputs);
								}
							}
						}
					}
				}
			}
			catch (IOException ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}

			Thread.Sleep(15); // Sleep for a while before checking for new connections/messages.
		}
	}

	public void RunReceiverGunD()
	{

		while (shouldStopReceiver == false)
		{
			try
			{
				using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("MameHookerProxyRecoilGunD", PipeDirection.In))
				{
					WaitForConnectionGunD = true;
					pipeServer.WaitForConnection();
					WaitForConnectionGunD = false;
					using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
					{
						char[] buffer = new char[4096];
						while (true)
						{
							if (shouldStopReceiver) return;

							int bytesRead = reader.Read(buffer, 0, buffer.Length);

							if (bytesRead == 0)
								break; // Fin du flux, sortir de la boucle

							string message = new string(buffer, 0, bytesRead);
							long timestamp = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

							int gunshot = 0;
							foreach (var lines in message.Split('\n'))
							{
								if (lines == "1") gunshot = 1;
								//Console.WriteLine("GUN D : " + lines);
							}
							if (_Outputs[4].OutputValue != gunshot)
							{
								_Outputs[4].OutputValue = gunshot;
								this.SendValues(_Outputs);
								if (gunshot == 1)
								{
									Thread.Sleep(25);
									_Outputs[4].OutputValue = 0;
									this.SendValues(_Outputs);
								}
							}
						}
					}
				}
			}
			catch (IOException ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}

			Thread.Sleep(15); // Sleep for a while before checking for new connections/messages.
		}
	}
		private void QuitApp()
		{
			shouldStopReceiver = true;

			if (WaitForConnectionGunA)
			{
				NamedPipeClientStream clt = new NamedPipeClientStream(".", "MameHookerProxyRecoilGunA", PipeDirection.Out);
				clt.Connect();
				clt.Close();
			}
			receiverThreadGunA.Join();

			if (WaitForConnectionGunB)
			{
				NamedPipeClientStream clt = new NamedPipeClientStream(".", "MameHookerProxyRecoilGunB", PipeDirection.Out);
				clt.Connect();
				clt.Close();
			}
			receiverThreadGunB.Join();

			if (WaitForConnectionGunC)
			{
				NamedPipeClientStream clt = new NamedPipeClientStream(".", "MameHookerProxyRecoilGunC", PipeDirection.Out);
				clt.Connect();
				clt.Close();
			}
			receiverThreadGunC.Join();

			if (WaitForConnectionGunD)
			{
				NamedPipeClientStream clt = new NamedPipeClientStream(".", "MameHookerProxyRecoilGunD", PipeDirection.Out);
				clt.Connect();
				clt.Close();
			}
			receiverThreadGunD.Join();

			Stop();
			Application.Exit();

		}


	/// <summary>
	/// A la fermeture de la fenêtre, envoie de la commande MAMEStop
	/// </summary>
	private void WndMain_FormClosing(object sender, FormClosingEventArgs e)
	{
	  Stop();
	}

	/// <summary>
	/// Interception des messages système Windows
	/// Pour traiter les messages reçu par MameHooker ou un autre client
	/// </summary>
	protected override void WndProc(ref Message m)
	{
	  // Lorsqu'un client (ex: MameHooker) envoie un message de suscription à la connection
	  // WParam contient le Handle du client
	  // LParam contient un identifiant
	  if (m.Msg == _Mame_RegisterClientMsg)
	  {
		RegisterClient(m.WParam, (UInt32)(m.LParam));
	  }

	  // Lorsqu'un client (ex: MameHooker) envoie un message de désinscription à la connection
	  // WParam contient le Handle du client
	  // LParam contient un identifiant
	  else if (m.Msg == _Mame_UnregisterClientMsg)
	  {
		UnregisterClient(m.WParam, (UInt32)(m.LParam));
	  }

	  // Lorsqu'un client (ex: MameHooker) envoie un message pour obtenir le nom d'une Output
	  // WParam contient le Handle du client
	  // LParam contient l'Id de l'Output dont le nom est demandé            
	  else if (m.Msg == _Mame_GetIdStringMsg)
	  {
		uint Id = (uint)m.LParam;
		// Si l'Id est 0, la requete porte sur le nom de la rom
		if (Id == 0)
		{
		  SendIdString(m.WParam, GameName, 0);
		}
		// Sinon, Le nom a envoyer est le libellé de l'Output
		else
		{
		  //Il faut chercher l'Output dont l'ID correspond à LParam si il y a une liste de plusieurs Outputs
		  foreach (GameOutput o in _Outputs)
		  {
			if (o.Id == (UInt32)m.LParam)
			{
			  SendIdString(m.WParam, o.Name, o.Id);
			  break;
			}
		  }
		}
	  }

	  // Renvoi des autres messages inutilisés vers la boucle originale pour le traitement des autres messages système
	  base.WndProc(ref m);
	}

	/// <summary>
	/// Définitions des messages MAME spécifique pour les échanges inter-processus
	/// </summary>
	private uint RegisterMameOutputMessage(String lpString)
	{
	  uint id = RegisterWindowMessage(lpString);
	  if (id == 0)
		MessageBox.Show("Error registering the following MameHooker message : " + lpString, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
	  return id;
	}

	#region Commandes MameOutput

	/// <summary>
	/// Envoi en Broadcast d'un message "MAMEOutput_Start" pour informer tous les clients potentiels
	/// </summary>
	public void Start()
	{
	  PostMessage(HWND_BROADCAST, _Mame_OnStartMsg, _hWnd, IntPtr.Zero);
	}

	/// <summary>
	/// Envoi en Broadcast d'un message "MAMEOutput_Stop" pour informer tous les clients potentiels
	/// </summary>
	public void Stop()
	{
	  PostMessage(HWND_BROADCAST, _Mame_OnStopMsg, _hWnd, IntPtr.Zero);
	}

	/// <summary>
	/// Ajout (ou mise à jour si déjà présent) d'un client dans la liste _RegisteredClients
	/// </summary>
	/// <param name="hWnd">Client Handle</param>
	/// <param name="Id">Client Id</param>
	public int RegisterClient(IntPtr hWnd, UInt32 Id)
	{
	  for (int i = 0; i < _RegisteredClients.Count; i++)
	  {
		if (_RegisteredClients[i].Id == Id)
		{
		  OutputClient c = _RegisteredClients[i];
		  c.hWnd = hWnd;
		  _RegisteredClients[i] = c;
		  return 1;
		}
	  }

	  OutputClient NewClient = new OutputClient();
	  NewClient.hWnd = hWnd;
	  NewClient.Id = Id;
	  _RegisteredClients.Add(NewClient);
	  return 0;
	}

	/// <summary>
	/// Supression d'un client MameOutput ayant l'ID correspondant
	/// </summary>
	/// <param name="hWnd">Client Handle</param>
	/// <param name="Id">Client ID</param>
	public void UnregisterClient(IntPtr hWnd, UInt32 Id)
	{
	  for (int i = _RegisteredClients.Count - 1; i >= 0; i--)
	  {
		if (_RegisteredClients[i].Id == Id)
		{
		  _RegisteredClients.RemoveAt(i);
		}
	  }
	}

	/// <summary>
	/// Répondre à un client qui envoie une requètre String/ID
	/// MameHooker envoie une requete dont Id=0 pour obtenir le nom de la rom
	/// Les autres Id sont le nom de l'Output associé
	/// </summary>
	/// <param name="hWnd">client hwnd</param>
	/// <param name="Id">Requested Id</param>
	public void SendIdString(IntPtr hWnd, String lpStr, UInt32 Id)
	{
	  OutputDataStruct data = new OutputDataStruct();
	  data.Id = Id;
	  data.lpStr = lpStr;
	  IntPtr buffer = IntPtrAlloc(data);
	  CopyDataStruct copyData = new CopyDataStruct();
	  copyData.dwData = new IntPtr(COPYDATA_MESSAGE_ID_STRING);
	  copyData.lpData = buffer;
	  copyData.cbData = Marshal.SizeOf(data);
	  IntPtr copyDataBuff = IntPtrAlloc(copyData);
	  SendMessage(hWnd, WM_COPYDATA, _hWnd, copyDataBuff);
	  IntPtrFree(ref copyDataBuff);
	  IntPtrFree(ref buffer);
	}

	/// <summary>
	/// Envoie de toutes les Outputs mise à jour, à tous les clients connectés
	/// Pour limiter les soucis, avec MameHooker, l'envoie du message d'Update d'une Output n'est effetué que s'il y a eu du changement dans sa valeur
	/// Ou lors de l'envoi original ( la première fois)
	/// </summary>
	/// <param name="Outputs">List of values to send</param>
	public void SendValues(List<GameOutput> Outputs)
	{
	  if (_FirstOutputs)
	  {
		//MameHooker compatibility : Sending orientation once
		Outputs.Insert(0, new GameOutput(MAME_ORIENTATION_STRING, MAME_ORIENTATION_ID));
		Outputs[0].OutputValue = 0;

		//Clonage de la liste des Outputs pour tester les differences de valeur.
		_OutputsBefore = Outputs.ConvertAll(x => new GameOutput(x));
		for (int i = 0; i < Outputs.Count; i++)
		{
		  SendValue(Outputs[i].Id, Outputs[i].OutputValue);
		}
		_FirstOutputs = false;
	  }
	  else
	  {
		for (int i = 0; i < Outputs.Count; i++)
		{
		  if (Outputs[i].OutputValue != _OutputsBefore[i].OutputValue)
		  {
			SendValue(Outputs[i].Id, Outputs[i].OutputValue);
			_OutputsBefore[i].OutputValue = Outputs[i].OutputValue;
		  }
		}
	  }
	}

	/// <summary>
	/// Envoie d'une valeur d'Output à tous les clients connectés
	/// </summary>
	public void SendValue(uint Id, int Value)
	{
	  foreach (OutputClient c in _RegisteredClients)
	  {
		PostMessage(c.hWnd, _Mame_UpdateStateMsg, new IntPtr(Id), new IntPtr(Value));
	  }
	}

	#endregion

	private void Txt_RomName_TextChanged(object sender, EventArgs e)
	{

	}

	private void WndMain_Load(object sender, EventArgs e)
	{

	}

	private void timer1_Tick(object sender, EventArgs e)
	{
	  timer1.Enabled = false;
	  SendValues(_Outputs);
	}

	private void Rdo1_On_CheckedChanged(object sender, EventArgs e)
	{

	}

	private void panel1_Paint(object sender, PaintEventArgs e)
	{

	}

  }



}
