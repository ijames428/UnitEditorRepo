using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;

namespace UnitEditor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		bool isDataDirty = false;
		string currentlySelectedState = "";
		//bool saveButtonWasEnabled = false;

		Bestiary MyBestiary;
		Unit SelectedUnit;

		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
			Show();

			MyBestiary = new Bestiary();
		}

		private void New(object sender, RoutedEventArgs e)
		{
			if (isDataDirty)
			{
				string msg = "Data is dirty. Start new bestiary without saving?";

				MessageBoxResult result =
				  MessageBox.Show(
					msg,
					"Data App",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);
				if (result == MessageBoxResult.Yes)
				{
					MakeUserEnterNewBestiaryName();
				}
			}
			else
			{
				MakeUserEnterNewBestiaryName();
			}
		}

		void MakeUserEnterNewBestiaryName()
		{
			BestiaryNameLabel.Visibility = Visibility.Hidden;
			BestiaryNameTextBox.Visibility = Visibility.Visible;
			ConfirmNameButton.Visibility = Visibility.Visible;
			CancelNameButton.Visibility = Visibility.Visible;

			BestiaryNameTextBox.Text = "";
			BestiaryNameTextBox.Focus();

			//saveButtonWasEnabled = SaveButton.IsEnabled;
		}

		void CancelNewBestiary()
		{
			//SaveButton.IsEnabled = saveButtonWasEnabled;

			BestiaryNameLabel.Visibility = Visibility.Visible;
			BestiaryNameTextBox.Visibility = Visibility.Hidden;
			ConfirmNameButton.Visibility = Visibility.Hidden;
			CancelNameButton.Visibility = Visibility.Hidden;
		}

		void CreateNewBestiary()
		{
			MyBestiary = new Bestiary();

			isDataDirty = true;
			SaveButton.IsEnabled = true;

			BestiaryNameLabel.Content = BestiaryNameTextBox.Text;

			BestiaryNameLabel.Visibility = Visibility.Visible;
			BestiaryNameTextBox.Visibility = Visibility.Hidden;
			ConfirmNameButton.Visibility = Visibility.Hidden;
			CancelNameButton.Visibility = Visibility.Hidden;

			CreateUnitButton.IsEnabled = true;
		}

		private void OnUnitSelected(object sender, RoutedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			string name = item.Content.ToString();

			DeselectUnit();
			SelectUnit(name);
		}

		private void OnUnitUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void OnStateSelected(object sender, RoutedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			currentlySelectedState = item.Content.ToString();

			List<StateAnimation> unitsStateAnimationList = new List<StateAnimation>();

			if (currentlySelectedState == "Idle")
			{
				unitsStateAnimationList = SelectedUnit.IdleAnimations;
			}
			else if (currentlySelectedState == "Walk")
			{
				unitsStateAnimationList = SelectedUnit.WalkAnimations;
			}

			ListOfStateAnimations.Items.Clear();
			FrameCanvas.Children.Clear();

			for (int i = 0; i < unitsStateAnimationList.Count; i++)
			{
				ListBoxItem stateAnimationItem = new ListBoxItem();
				stateAnimationItem.Content = unitsStateAnimationList[i].FilePath;
				stateAnimationItem.Selected += OnAnimationSelected;
				stateAnimationItem.Unselected += OnAnimationUnselected;

				ListOfStateAnimations.Items.Add(stateAnimationItem);

				numberOfFramesTextBox.Text = unitsStateAnimationList[i].NumberOfFrames.ToString();
				frameWidthTextBox.Text = unitsStateAnimationList[i].FrameDimensionsX.ToString();
				frameHeightTextBox.Text = unitsStateAnimationList[i].FrameDimensionsY.ToString();

				OpenAndShowImage(unitsStateAnimationList[i].FilePath);
			}

			if (ListOfStateAnimations.Items.Count > 0)
			{
				ListBoxItem stateAnimationItem = (ListBoxItem)ListOfStateAnimations.Items[0];
				stateAnimationItem.IsSelected = true;
			}

			//DeselectUnit();
			//SelectUnit(name);
		}

		private void OnStateUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void OnAnimationSelected(object sender, RoutedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			string name = item.Content.ToString();

			List<StateAnimation> unitsStateAnimationList = new List<StateAnimation>();
			
			if (currentlySelectedState == "Idle")
			{
				unitsStateAnimationList = SelectedUnit.IdleAnimations;
			}
			else if (currentlySelectedState == "Walk")
			{
				unitsStateAnimationList = SelectedUnit.WalkAnimations;
			}

			//ListOfStateAnimations.Items.Clear();

			for (int i = 0; i < unitsStateAnimationList.Count; i++)
			{
				if (name == unitsStateAnimationList[i].FilePath)
				{
					numberOfFramesTextBox.Text = unitsStateAnimationList[i].NumberOfFrames.ToString();
					frameWidthTextBox.Text = unitsStateAnimationList[i].FrameDimensionsX.ToString();
					frameHeightTextBox.Text = unitsStateAnimationList[i].FrameDimensionsY.ToString();
				}
			}

			//DeselectUnit();
			//SelectUnit(name);
		}

		private void OnAnimationUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void DeselectUnit()
		{
			ListOfStates.IsEnabled = false;
		}

		private void SelectUnit(string name)
		{
			if (MyBestiary.DictOfUnits.ContainsKey(name))
			{
				SelectedUnit = MyBestiary.DictOfUnits[name];

				ListOfStates.IsEnabled = true;
				ListBoxItem item = (ListBoxItem)ListOfStates.Items[0];
				item.IsSelected = true;
			}
		}

		void CreateUnit(object sender, RoutedEventArgs e)
		{
			ListOfUnits.IsEnabled = true;

			string unitName = "Unit0";
			int unitIndex = 0;

			while (MyBestiary.DictOfUnits.ContainsKey(unitName))
			{
				unitIndex++;
				unitName = "Unit" + unitIndex;
			}

			Unit newUnit = new Unit();
			newUnit.Name = unitName;

			MyBestiary.DictOfUnits.Add(newUnit.Name, newUnit);

			ListBoxItem item = new ListBoxItem();
			item.Content = unitName;
			item.Selected += OnUnitSelected;
			item.Unselected += OnUnitUnselected;
			item.IsSelected = true;
			ListOfUnits.Items.Add(item);

			SelectUnit(newUnit.Name);
		}

		void DeleteUnit(object sender, RoutedEventArgs e)
		{
			if (ListOfUnits.Items.Count == 0)
			{
				ListOfUnits.IsEnabled = false;
			}
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			string jsonStr = JsonConvert.SerializeObject(MyBestiary);

			Stream myStream;
			SaveFileDialog saveFileDialog = new SaveFileDialog();

			saveFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			saveFileDialog.FilterIndex = 2;
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.FileName = BestiaryNameLabel.Content.ToString() + ".txt";

			if (saveFileDialog.ShowDialog() == true)
			{
				if ((myStream = saveFileDialog.OpenFile()) != null)
				{
					StreamWriter writer = new StreamWriter(myStream);

					writer.Write(jsonStr);
					writer.Flush();
					myStream.Position = 0;

					myStream.Close();

					isDataDirty = false;
				}
			}
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			string result = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog.FileName);

				result = sr.ReadToEnd();

				sr.Close();
			}

			if (!string.IsNullOrWhiteSpace(result))
			{
				Bestiary bestiary = JsonConvert.DeserializeObject<Bestiary>(result);
				BestiaryNameLabel.Content = bestiary.Name;

				FrameCanvas.Children.Clear();

				isDataDirty = false;

				CreateUnitButton.IsEnabled = true;
			}
		}

		void DataWindow_Closing(object sender, CancelEventArgs e)
		{
			if (isDataDirty)
			{
				string msg = "Data is dirty. Close without saving?";

				MessageBoxResult result =
				  MessageBox.Show(
					msg,
					"Data App",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning);
				if (result == MessageBoxResult.No)
				{
					// If user doesn't want to close, cancel closure
					e.Cancel = true;
				}
			}
		}

		private void ConfirmNameButton_Click(object sender, RoutedEventArgs e)
		{
			CreateNewBestiary();
		}

		private void CancelNameButton_Click(object sender, RoutedEventArgs e)
		{
			CancelNewBestiary();
		}

		private void OnKeyDownHandler(object sender, KeyEventArgs e)
		{
			if (BestiaryNameTextBox.Visibility == Visibility.Visible)
			{
				if (e.Key == Key.Return)
				{
					CreateNewBestiary();
				}
				if (e.Key == Key.Escape)
				{
					CancelNewBestiary();
				}
			}
		}

		//private void StateSelected(object sender, RoutedEventArgs e)
		//{
		//	ListBoxItem item = (ListBoxItem)sender;
		//	currentlySelectedState = item.Content.ToString();
		//}

		private void AddAnimationButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			//openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				BitmapImage bitmap = OpenAndShowImage(openFileDialog.FileName);

				StateAnimation newStateAnimation = new StateAnimation();
				newStateAnimation.FilePath = openFileDialog.FileName;
				newStateAnimation.NumberOfFrames = 1;
				newStateAnimation.FrameDimensionsX = 10;
				newStateAnimation.FrameDimensionsY = 10;
				newStateAnimation.SourceDimensionsX = (int)bitmap.PixelWidth;
				newStateAnimation.SourceDimensionsX = (int)bitmap.PixelHeight;

				numberOfFramesTextBox.Text = newStateAnimation.NumberOfFrames.ToString();
				frameWidthTextBox.Text = newStateAnimation.FrameDimensionsX.ToString();
				frameHeightTextBox.Text = newStateAnimation.FrameDimensionsY.ToString();

				ListBoxItem item = new ListBoxItem();
				item.Content = newStateAnimation.FilePath;
				item.Selected += OnAnimationSelected;
				item.Unselected += OnAnimationUnselected;
				item.IsSelected = true;

				ListOfStateAnimations.Items.Add(item);
				ListOfStateAnimations.IsEnabled = true;

				AddStateAnimationToSelectedUnitsState(newStateAnimation);
			}
		}

		private BitmapImage OpenAndShowImage(string filepath)
		{
			BitmapImage bitmap = new BitmapImage(new Uri(filepath, UriKind.Absolute));
			Image image = new Image();

			double maxX = FrameCanvas.ActualWidth / 2.0 - bitmap.Width / 2.0;
			double maxY = FrameCanvas.ActualHeight / 2.0 - bitmap.Height / 2.0;

			image.Source = bitmap;
			image.Width = bitmap.Width;
			image.Height = bitmap.Height;
			Canvas.SetLeft(image, maxX);
			Canvas.SetTop(image, maxY);
			FrameCanvas.Children.Add(image);

			return bitmap;
		}

		private void AddStateAnimationToSelectedUnitsState(StateAnimation stateAnimation)
		{
			//ListBoxItem item = (ListBoxItem)ListOfStates.SelectedItem;

			if (currentlySelectedState == "Idle")
			{
				SelectedUnit.IdleAnimations.Add(stateAnimation);
			}
			else if (currentlySelectedState == "Walk")
			{
				SelectedUnit.WalkAnimations.Add(stateAnimation);
			}
		}
	}

	public class Bestiary
	{
		public string Name = "";
		public Dictionary<string, Unit> DictOfUnits = new Dictionary<string, Unit>();
		//public List<Unit> Units;

		public Bestiary()
		{
		}
	}

	public class Unit
	{
		public string Name = "";
		public List<StateAnimation> IdleAnimations = new List<StateAnimation>();
		public List<StateAnimation> WalkAnimations = new List<StateAnimation>();

		public Unit()
		{
		}
	}

	public class StateAnimation
	{
		public string State = "";
		public string FilePath = "";
		public int NumberOfFrames = 0;
		public int SourceDimensionsX = 0;
		public int SourceDimensionsY = 0;
		public int FrameDimensionsX = 0;
		public int FrameDimensionsY = 0;

		public StateAnimation()
		{
		}
	}
}
