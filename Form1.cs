﻿//using Newtonsoft.Json;
using DownloaderAllTheLinks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloaderAllTheLinks
{
	public partial class Form1 : Form
	{
		private int sortColumn = -1;
		//RootObject root = null;

		List<string> office = new List<string> { "pdf", "doc", "docx", "xls", "xlsx", "odt", "rtf", "txt", "ppt", "pptx", "ods","xps","csv","html", "shtml"};
		List<string> images = new List<string> { "png", "tiff", "jpg", "jpeg", "gif", "ico"};
		List<string> archives = new List<string> { "rar", "zip", "tar", "7z"};

		string placeholder = "placeholder";
		string regPattern = @"(<a.+href=""([^\""]+\.?(placeholder)?){1}\""([^>]*)>((?:.(?!\<\/a\>))*.?)<\/a>)";
		string alternateRegPattern = @"(<a([\s\S](?!>))*?title=""(([\s\S](?!>))*?(placeholder))[\s\S]*?""[\s\S]*?>)";

		public Form1()
		{
			InitializeComponent();

			string downloadsPath = AppDomain.CurrentDomain.BaseDirectory + "Downloads";
			if (!Directory.Exists(downloadsPath))
			{
				Directory.CreateDirectory(downloadsPath);
			}
			FolderBox.Text = downloadsPath;
			
		}

		private void LoadWebPage(string url)
		{
			using (WebClient client = new WebClient())
			{
				client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
				client.Encoding = Encoding.UTF8;
				client.DownloadStringCompleted += Client_DownloadStringCompleted;
				client.DownloadStringAsync(new Uri(url));
			}
		}

		private void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			PopulateList(e.Result);
		}

		private void PopulateList(string text)
		{
			listView1.Items.Clear();

			Hashtable links = new Hashtable();

			List<string> fileTypes = GetFileTypes(); 

			string CurrentRegPattern = regPattern.Replace(placeholder, String.Join("|", fileTypes));
			Regex reg = new Regex(CurrentRegPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			Uri host = new Uri(textBox1.Text);
			foreach (Match m in reg.Matches(text))
			{
				string link = m.Groups[2].Value;
				string fullLink = "";
				if (link.StartsWith("/"))
					fullLink = host.Scheme + Uri.SchemeDelimiter + host.Host + link;
				else if (link.StartsWith("http"))
					fullLink = link;
				else
					fullLink = host.Scheme + Uri.SchemeDelimiter + host.Host + "/" + link;

				ListViewItem lvi = new ListViewItem(fullLink);
				if (UseFileNamesBox.Checked)
					lvi.SubItems.Add(new Uri(fullLink).Segments.LastOrDefault());
				else
					lvi.SubItems.Add(m.Groups[5].Value);
				lvi.Checked = true;
				listView1.Items.Add(lvi); 
			}

			// variant 2
			if (alternateWayBox.Checked)
			{
				CurrentRegPattern = alternateRegPattern.Replace(placeholder, String.Join("|", fileTypes));
				reg = new Regex(CurrentRegPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				host = new Uri(textBox1.Text);
				foreach (Match m in reg.Matches(text))
				{
					string link = m.Groups[1].Value;
					if (link.Contains("href"))
					{
						MatchCollection coll = Regex.Matches(link, @"(href="")(([\s\S])+?)("")", RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
						foreach (Match mx in coll)
						{
							if (mx.Groups[2].Value.Contains("http"))
							{
								link = mx.Groups[2].Value;
								break;
							}
						}
					}
					string fullLink = "";
					if (link.StartsWith("/"))
						fullLink = host.Scheme + Uri.SchemeDelimiter + host.Host + link;
					else if (link.StartsWith("http"))
						fullLink = link;
					else
						fullLink = host.Scheme + Uri.SchemeDelimiter + host.Host + "/" + link;

					if (!links.ContainsKey(fullLink))
					{
						ListViewItem lvi = new ListViewItem(fullLink);
						if (m.Groups[3].Value.Length > 0)
							lvi.SubItems.Add(m.Groups[3].Value);
						else if (UseFileNamesBox.Checked)
							lvi.SubItems.Add(new Uri(fullLink).Segments.LastOrDefault());
						else
							lvi.SubItems.Add(m.Groups[5].Value);
						lvi.Checked = true;
						listView1.Items.Add(lvi);
						links[fullLink] = m.Groups[3].Value;
					}

				}
			}
		}

		private List<string> GetFileTypes()
		{
			List<string> fileTypes = new List<string>();
			if (OfficeBox.Checked)
				fileTypes = fileTypes.Concat(office).ToList();
			if (ArchivesBox.Checked)
				fileTypes = fileTypes.Concat(archives).ToList();
			if (ImagesBox.Checked)
				fileTypes = fileTypes.Concat(images).ToList();

			return fileTypes;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			StatusBox.Text = "";
			if (textBox1.Text.Length != 0)
			{
				LoadWebPage(textBox1.Text);
			}
			else
			{
				MessageBox.Show("You must specify URL!");
			}
		}

		private async void button2_Click(object sender, EventArgs e)
		{
			if (!Directory.Exists(FolderBox.Text))
			{
				Directory.CreateDirectory(FolderBox.Text);
			}
			await GrabThemAll();
			MessageBox.Show("Finished");
			StatusBox.Text = "Finished!";
		}

		private async Task GrabThemAll()
		{
			int count = 0;
			ProgressBox.Value = 0;
			ProgressBox.Maximum = 0;
			StatusBox.Text = "Downloading..";
			foreach (ListViewItem lvi in listView1.Items)
			{
				if (lvi.Checked)
				{
					ProgressBox.Maximum++;
				}
			}
			string dir = FolderBox.Text;
			Dictionary<string, string> urls = new Dictionary<string, string>();
			foreach (ListViewItem lvi in listView1.Items)
			{
				if (lvi.Checked)
				{
					
					count++;
					string localFile = dir + "\\" + lvi.SubItems[1].Text + (!UseFileNamesBox.Checked && !lvi.Text.EndsWith("/") && !lvi.Text.Substring(lvi.Text.LastIndexOf('.')).Contains("/") ? ("." + lvi.Text.Substring(lvi.Text.LastIndexOf('.'))) : "");
					if (UseFileNamesBox.Checked)
					{
						localFile = dir + "\\" + lvi.SubItems[1].Text;
					}
					localFile = localFile.Replace("...", ".").Replace("..", ".");
					if (File.Exists(localFile))
					{
						File.Delete(localFile);
					}
					string UriString = lvi.Text;
					urls[UriString] = localFile;

					
					using (WebClient client = new WebClient())
					{
						client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
						await client.DownloadFileTaskAsync(new Uri(UriString), localFile);
						UpdateProgress(1);
						//client.DownloadFile(new Uri(UriString), localFile);
					}
				}
				//break;
			}

			/*
			var tasks = urls
						   .Select(url => Task.Factory.StartNew(
							   state =>
							   {
								   using (WebClient client = new WebClient())
								   {
									   try
									   {
										   client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
										   client.DownloadFile(new Uri(url.Key), url.Value);

										   //this.InvokeIfRequired(c => { c.UpdateProgress(1); });
									   }
									   catch
									   { }
									   finally
									   {
										   UpdateProgress(1);
									   }
									   //client.DownloadFile(new Uri(UriString), localFile);
								   }
							   }, url)
						   )
						   .ToArray();

			Task.WaitAll(tasks);
			*/
		}

		public void UpdateProgress(int v)
		{
			/*
			ProgressBox.InvokeIfRequired(() =>
			{
				// Do anything you want with the control here
				ProgressBox.RtfText = value;
				RtfHelpers.AddMissingStyles(richEditControl1);
			});
			*/
			if (this.InvokeRequired)
			{
				this.Invoke(new Action(() => UpdateProgress(v)));
			}
			else
			{
				ProgressBox.Value += v;
			}
			
		}

		
		/*
		public static void InvokeIfRequired1(this Control obj,
										 MethodInvoker action)
		{
			if (obj.InvokeRequired)
			{
				var args = new object[0];
				obj.Invoke(action, args);
			}
			else
			{
				action();
			}
		}
		*/

		/*
		public static T GetObject<T>(string text)
		{
			T obj = JsonConvert.DeserializeObject<T>(text);
			return obj;
		}
		*/

		private void CheckAllBtn_Click(object sender, EventArgs e)
		{
			SetCheckState(true);
		}

		private void InvertCheckState()
		{
			foreach (ListViewItem lvi in listView1.SelectedItems)
			{
				lvi.Checked = !lvi.Checked;
			}
		}

		private void SetCheckState(bool v)
		{
			foreach (ListViewItem lvi in listView1.Items)
			{
				lvi.Checked = v;
			}
		}

		private void UncheckAllBtn_Click(object sender, EventArgs e)
		{
			SetCheckState(false);
		}

		private void button3_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();

			if (fbd.ShowDialog() == DialogResult.OK)
			{
				if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
				{
					FolderBox.Text = fbd.SelectedPath;
				}
			}
		}

		private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// Determine whether the column is the same as the last column clicked.
			if (e.Column != sortColumn)
			{
				// Set the sort column to the new column.
				sortColumn = e.Column;
				// Set the sort order to ascending by default.
				listView1.Sorting = SortOrder.Ascending;
			}
			else
			{
				// Determine what the last sort order was and change it.
				if (listView1.Sorting == SortOrder.Ascending)
					listView1.Sorting = SortOrder.Descending;
				else
					listView1.Sorting = SortOrder.Ascending;
			}

			// Call the sort method to manually sort.
			listView1.Sort();
			// Set the ListViewItemSorter property to a new ListViewItemComparer
			// object.
			this.listView1.ListViewItemSorter = new ListViewItemComparer(e.Column, listView1.Sorting);
		}

		private void InvertBtn_Click(object sender, EventArgs e)
		{
			InvertCheckState();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			
			ProcessStartInfo StartInformation = new ProcessStartInfo();

			StartInformation.FileName = FolderBox.Text;

			Process process = Process.Start(StartInformation);

			//process.EnableRaisingEvents = true;
		}
	}

	public class Link
	{
		public string Url { get; set; }
		public string Name { get; set; }
	}

	public class RootObject
	{
		public List<Link> Links { get; set; }
	}

	public class ListViewItemComparer : IComparer
	{

		private int col;
		private SortOrder order;
		public ListViewItemComparer()
		{
			col = 0;
			order = SortOrder.Ascending;
		}
		public ListViewItemComparer(int column, SortOrder order)
		{
			col = column;
			this.order = order;
		}
		public int Compare(object x, object y)
		{
			int returnVal = -1;
			returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text,
							((ListViewItem)y).SubItems[col].Text);
			// Determine whether the sort order is descending.
			if (order == SortOrder.Descending)
				// Invert the value returned by String.Compare.
				returnVal *= -1;
			return returnVal;
		}
	}
}
