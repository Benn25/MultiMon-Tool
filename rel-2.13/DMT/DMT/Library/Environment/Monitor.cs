#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015-2026  Gerald Evans
// 
// Dual Monitor Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DMT.Library.Environment
{
	/// <summary>
	/// Replacement of System.Windows.Forms.Screen
	/// Mainly so we can control our own ordering of the screens
	/// </summary>
	public class Monitor
	{
		public enum EMonitorOrder { DotNet = 1, DisplayName = 2, LeftRight = 3, TopDown = 4 };

		Screen _screen;

		static EMonitorOrder _monitorOrder = EMonitorOrder.DotNet;	// TODO
		public static EMonitorOrder MonitorOrder
		{
			get
			{
				return _monitorOrder;
			}
			set
			{
				_monitorOrder = value;
				ReBuild();
			}
		}

		static string[] _monitorNameOrder = null;
		public static string[] MonitorNameOrder
		{
			get
			{
				return _monitorNameOrder;
			}
			set
			{
				_monitorNameOrder = value;
				// only need to rebuild if currently viewing in name order
				if (_monitorOrder == EMonitorOrder.DisplayName)
				{
					ReBuild();
				}
			}
		}

		static DMT.Library.DisplayHandling.MonitorNameIdMap _monitorNames = null;
		public static DMT.Library.DisplayHandling.MonitorNameIdMap MonitorNames
		{
			get
			{
				return _monitorNames;
			}
			set
			{
				_monitorNames = value;
				if (_monitorOrder == EMonitorOrder.DisplayName)
				{
					ReBuild();
				}
			}
		}

		/// <summary>
		/// Gets or sets the bounds for the monitor
		/// </summary>
		public Rectangle Bounds
		{
			get
			{
				return _screen.Bounds;
			}
		}

		/// <summary>
		/// Gets or sets the working area for the monitor
		/// </summary>
		public Rectangle WorkingArea 
		{
			get
			{
				return _screen.WorkingArea;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this is the primary monitor
		/// </summary>
		public bool Primary
		{
			get
			{
				return _screen.Primary;
			}
		}

		/// <summary>
		/// To simplify replacement of Screen
		/// May remove this in time?
		/// </summary>
		public Screen Screen
		{
			get
			{
				return _screen;
			}
		}


		public Monitor(Screen screen)
		{
			_screen = screen;
		}

		public bool ContainsPoint(Point point)
		{
			return (point.X >= Bounds.Left && point.X < Bounds.Right
					&& point.Y >= Bounds.Top && point.Y < Bounds.Bottom);
		}

		public int DistanceFromPoint(Point point)
		{
			int x_distance = 0;
			int y_distance = 0;

			if (point.X < Bounds.Left)
			{
				x_distance = Bounds.Left - point.X;
			}
			else if (point.X >= Bounds.Right)
			{
				x_distance = point.X - Bounds.Right + 1;
			}


			if (point.Y < Bounds.Top)
			{
				y_distance = Bounds.Top - point.Y;
			}
			else if (point.Y >= Bounds.Bottom)
			{
				y_distance = point.Y - Bounds.Bottom + 1;
			}

			// alt: return diagonal distance (or the square of it)
			return x_distance + y_distance;
		}


		static Monitors _allMonitors = null;
		static int[] _toScreenIdx;

		public static Monitors AllMonitors
		{
			get
			{
				if (_allMonitors == null)
				{
					// TODO: want to throw?
					ReBuild();
				}

				return _allMonitors;
			}
		}

		public static void ReBuild()
		{
			Screen[] allScreens = Screen.AllScreens;

			switch (_monitorOrder)
			{
				case EMonitorOrder.DotNet:
					BuildDotNetOrder();
					break;

				case EMonitorOrder.DisplayName:
					BuildNameOrder();
					break;

				case EMonitorOrder.TopDown:
					BuildSortedOrder(allScreens, new TopDownComparer(allScreens));
					break;

				case EMonitorOrder.LeftRight:  // treat as default case
				default:
					//BuildLeftRightOrder();
					BuildSortedOrder(allScreens, new LeftRightComparer(allScreens));
					break;
			}
		}

		public static bool ParseMonitorIndex(string parameters, out int monitorIndex)
		{
			int monitorNum;
			if (int.TryParse(parameters, out monitorNum))
			{
				// we have been given the numeric monitor number
				monitorIndex = monitorNum - 1;
				return true;
			}
			else if (parameters.Length > 0)
			{
				char firstChar = parameters[0];
				if (firstChar == 'x' || firstChar == 'X')
				{
					// want the monitor that the cursor is currently on
					monitorIndex = AllMonitors.MonitorIndexFromPoint(System.Windows.Forms.Cursor.Position);
					return true;
				}
			}

			monitorIndex = -1;
			return false;
		}

		static void BuildDotNetOrder()
		{
			Screen[] allScreens = Screen.AllScreens;

			// set up Monitor[] -> Screen[] map
			// direct mapping in this case
			int[] toScreenIdx = new int[allScreens.Length];
			for (int n = 0; n < allScreens.Length; n++)
			{
				toScreenIdx[n] = n;
			}

			BuildNewOrder(toScreenIdx, allScreens);
		}

		static void BuildSortedOrder(Screen[] allScreens, System.Collections.IComparer comparer)
		{
			// set up Monitor[] -> Screen[] map
			// start with direct mapping
			int[] toScreenIdx = new int[allScreens.Length];
			for (int n = 0; n < allScreens.Length; n++)
			{
				toScreenIdx[n] = n;
			}

			// now sort into required order using TLHCs
			Array.Sort(toScreenIdx, comparer);

			BuildNewOrder(toScreenIdx, allScreens);
		}

		static void BuildNewOrder(int[] toScreenIdx, Screen[] allScreens)
		{

			// and add the monitors in this order
			Monitors allMonitors = new Monitors(allScreens.Length);
			for (int n = 0; n < allScreens.Length; n++)
			{
				Screen screen = allScreens[toScreenIdx[n]];
				allMonitors.Add(new Monitor(screen));
			}

			// TODO: don't think we need to do this as an atomic operation?
			_toScreenIdx = toScreenIdx;
			_allMonitors = allMonitors;
		}

		static void BuildNameOrder()
		{
			Screen[] allScreens = Screen.AllScreens;

			// set up Monitor[] -> Screen[] map
			// start with direct mapping
			int[] toScreenIdx = new int[allScreens.Length];
			for (int n = 0; n < allScreens.Length; n++)
			{
				toScreenIdx[n] = n;
			}

			// now sort into left-right ordering
			Array.Sort(toScreenIdx, new LeftRightComparer(allScreens));

			// now impose the name ordering onto this
			SortByName(toScreenIdx, allScreens);

			BuildNewOrder(toScreenIdx, allScreens);
		}

		static void SortByName(int[] toScreenIdx, Screen[] allScreens)
		{
			// get the monitorIds for each screen in allScreens
			string[] monitorIds = GetMonitorIds(allScreens);

			// make sure we do have some data to work with
			if (_monitorNames != null)
			{
				if (_monitorNameOrder != null && _monitorNameOrder.Length > 0)
				{
					int insertIdx = 0; // next position to move a named monitor to 

					// for each ordered name that we have...
					foreach (string name in _monitorNameOrder)
					{
						// each monitor with this name should be moved to insertIndex
						for (int screenIdxIdx = insertIdx; screenIdxIdx < toScreenIdx.Length; screenIdxIdx++)
						{
							int screenIdx = toScreenIdx[screenIdxIdx];
							string monitorId = monitorIds[screenIdx];

							// see if the user has given it a name
							string monitorName = _monitorNames.GetName(monitorId);

							// if it matches the current name from the name order
							if (string.Compare(monitorName, name, true) == 0)
							{
								// adjust it's position in the list
								for (int i = screenIdxIdx; i > insertIdx; i--)
								{
									toScreenIdx[i] = toScreenIdx[i - 1];
								}
								toScreenIdx[insertIdx] = screenIdx;
								insertIdx++;
							}

							// keep checking as the user may have given multiple monitors the same name
						}
					}
				}
			}
		}

		public static string[] GetMonitorIds(Screen[] allScreens)
		{
			string[] monitorIds = new string[allScreens.Length];

			// TODO: this is slow as we get more than we need
			DMT.Library.DisplayHandling.DisplayDevices displayDevices = new DMT.Library.DisplayHandling.DisplayDevices(false);

			for (int screenIdx = 0; screenIdx < allScreens.Length; screenIdx++)
			{
				DMT.Library.DisplayHandling.DisplayDevice displayDevice = displayDevices.FindMonitor(allScreens[screenIdx].Bounds);
				if (displayDevice != null)
				{
					monitorIds[screenIdx] = displayDevice.DevicePath.ToString();
				}
				else
				{
					monitorIds[screenIdx] = "";
				}

			}

			return monitorIds;

		}

		public abstract class MonitorOrderComparer : System.Collections.IComparer
		{
			protected Screen[] _allScreens;
			public MonitorOrderComparer(Screen[] allScreens)
			{
				_allScreens = allScreens;
			}

			public abstract int Compare(Object a, Object b);

			protected Screen ObjectToScreen(Object o)
			{
				int idx = (int)o;
				if (idx < 0 || idx >= _allScreens.Length)
				{
					throw new ApplicationException(string.Format("MonitorOrderComparer.ObjectToScreen({0}) - invalid", idx));
				}

				return _allScreens[idx];
			}
		}

		public class TopDownComparer : MonitorOrderComparer
		{
			public TopDownComparer(Screen[] allScreens)
				: base(allScreens)
			{
			}

			public override int Compare(Object a, Object b)
			{
				Screen screenA = ObjectToScreen(a);
				Screen screenB = ObjectToScreen(b);

				if (screenA.Bounds.Top < screenB.Bounds.Top)
				{
					return -1;
				}
				else if (screenA.Bounds.Top > screenB.Bounds.Top)
				{
					return 1;
				}
				else
				{
					// use left right if tops are the same
					if (screenA.Bounds.Left < screenB.Bounds.Left)
					{
						return -1;
					}
					else if (screenA.Bounds.Left > screenB.Bounds.Left)
					{
						return 1;
					}
				}

				// shouldn't get here
				return 0;
			}
		}

		public class LeftRightComparer : MonitorOrderComparer
		{
			public LeftRightComparer(Screen[] allScreens)
				: base(allScreens)
			{
			}

			public override int Compare(Object a, Object b)
			{
				Screen screenA = ObjectToScreen(a);
				Screen screenB = ObjectToScreen(b);

				if (screenA.Bounds.Left < screenB.Bounds.Left)
				{
					return -1;
				}
				else if (screenA.Bounds.Left > screenB.Bounds.Left)
				{
					return 1;
				}
				else
				{
					// use top down if lefts are the same
					if (screenA.Bounds.Top < screenB.Bounds.Top)
					{
						return -1;
					}
					else if (screenA.Bounds.Top > screenB.Bounds.Top)
					{
						return 1;
					}
				}

				// shouldn't get here
				return 0;
			}
		}
	}
}
