using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace RemapOMENKey
{
	class Program
	{
		static void Main() => new Program().Run();

		struct KEYBDINPUT
		{
			public uint type;
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public uint time;
			public uint dwExtraInfo;
			public uint unused1;
			public uint unused2;
		}

		static readonly int SIZEOF_KEYBDINPUT = Marshal.SizeOf(typeof(KEYBDINPUT));

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint SendInput(uint cInputs,
			[MarshalAs(UnmanagedType.LPArray), In] KEYBDINPUT[] inputs, int cbSize);

		[DllImport("user32.dll")]
		static extern uint MapVirtualKey(uint uCode, uint uMapType);

		readonly ManagementEventWatcher watcher = new ManagementEventWatcher(
			new ManagementScope("root\\WMI", null),
			new EventQuery("SELECT * FROM hpqBEvnt")
		);

		const ushort VK_HOME = 0x24;

		readonly KEYBDINPUT[] homeInputs = new KEYBDINPUT[] {
			new KEYBDINPUT {
				type = 1, // INPUT_KEYBOARD
				wVk = VK_HOME,
				dwFlags = 9, // KEYEVENTF_EXTENDEDKEY | KEYEVENTF_SCANCODE
				wScan = 0, time = 0, dwExtraInfo = 0,
				unused1 = 0, unused2 = 0,
			},
			new KEYBDINPUT(),
		};

		Program()
		{
			homeInputs[0].wScan = (ushort)MapVirtualKey(VK_HOME, 0 /*MAPVK_VK_TO_VSC*/);
			homeInputs[1] = homeInputs[0];
			homeInputs[1].dwFlags |= 2; // KEYEVENTF_KEYUP
			watcher.EventArrived += EventArrived;
		}

		void Run()
		{
			watcher.Start();

			foreach (Process p in Process.GetProcesses())
			{
				if (p.ProcessName.Equals("HP.Omen.OmenCommandCenter")) p.Kill();
				if (p.ProcessName.Equals("OmenCommandCenterBackground")) p.Kill();
			}
			Thread.Sleep(Timeout.Infinite);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031")]
		void EventArrived(object sender, EventArrivedEventArgs e)
		{
			try
			{
				if (!((uint)29).Equals(e.NewEvent["eventId"])) return;
				if (!((uint)8613).Equals(e.NewEvent["EventData"])) return;

				SendInput((uint)homeInputs.Length, homeInputs, SIZEOF_KEYBDINPUT);
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception);
			}
		}
	}
}
