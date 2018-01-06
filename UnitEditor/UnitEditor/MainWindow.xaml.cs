﻿using System;
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
using System.Text.RegularExpressions;

namespace UnitEditor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		bool isDataDirty = false;
		string UnitState = "";
		//int currentFrameCount = 0;
		int AnimationState = 0;
		int ANIMATION_STATE_PLAY = 0;
		int ANIMATION_STATE_PAUSED = 1;

		string previouslySelectedAnimationsName = "";

		HitOrHurtBox previouslySelectedHitOrHurtBox = null;
		
		SolidColorBrush HurtBoxColor = Brushes.Green;
		SolidColorBrush HitBoxColor = Brushes.Red;

		CroppedBitmap animatedSprite;
		Image spriteImage = new Image();
		BitmapImage bitmap;

		Bestiary MyBestiary;
		Unit SelectedUnit;
		
		System.Windows.Threading.DispatcherTimer updateTimer = new System.Windows.Threading.DispatcherTimer();
		System.Windows.Threading.DispatcherTimer drawTimer = new System.Windows.Threading.DispatcherTimer();

		Point mouse_pressed_pos = new Point();
		Rectangle rect_getting_drawn = null;

		private void Update(object sender, EventArgs e)
		{
			drawTimer.Interval = TimeSpan.FromSeconds(1.0 / (FramesPerSecond));

			if (UnitNameTextBox.IsFocused)
			{
				ListBoxItem item = (ListBoxItem)ListOfUnits.SelectedItem;
				item.Content = UnitNameTextBox.Text;
			}

			if (numberOfFramesTextBox.IsFocused)
			{
				ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
				if (selectedAnimationItem != null)
				{
					string selectedAnimationName = selectedAnimationItem.Content.ToString();
					StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

					if (selectedAnimation != null)
					{
						int numOfFrames = 0;

						int.TryParse(numberOfFramesTextBox.Text, out numOfFrames);

						selectedAnimation.NumberOfFrames = numOfFrames;
					}
				}
			}

			if (UnitNameTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					MyBestiary.DictOfUnits.Remove(SelectedUnit.UnitName);

					SelectedUnit.UnitName = UnitNameTextBox.Text;

					MyBestiary.DictOfUnits.Add(SelectedUnit.UnitName, SelectedUnit);
				}
			}

			if (HitPointsTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					int result = 0;
					int.TryParse(HitPointsTextBox.Text, out result);

					SelectedUnit.HitPoints = result;
				}
			}

			if (MovementSpeedTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					float result = 0.0f;
					float.TryParse(MovementSpeedTextBox.Text, out result);

					SelectedUnit.MovementSpeed = result;
				}
			}
			
			if (SelectedUnit != null)
			{
				StateAnimation selectedAnimation = GetCurrentStateAnimation();
				if (selectedAnimation != null)
				{
					if (ListOfHurtAndHitBoxes.SelectedItem != null)
					{
						ListBoxItem selectedHitOrHurtBoxItem = (ListBoxItem)ListOfHurtAndHitBoxes.SelectedItem;
						string selectedHitOrHurtBoxName = selectedHitOrHurtBoxItem.Content.ToString();
						HitOrHurtBox selectedHitOrHurtBox;
						bool isHitBox = selectedHitOrHurtBoxName.Contains("Hit");

						if (isHitBox)
						{
							selectedHitOrHurtBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedHitOrHurtBoxName).First();
						}
						else
						{
							selectedHitOrHurtBox = selectedAnimation.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedHitOrHurtBoxName).First();
						}

						if (selectedHitOrHurtBox != null)
						{
							try
							{
								int.TryParse(DamageTextBox.Text, out selectedHitOrHurtBox.Damage);
								float.TryParse(KnockBackXTextBox.Text, out selectedHitOrHurtBox.KnockBackX);
								float.TryParse(KnockBackYTextBox.Text, out selectedHitOrHurtBox.KnockBackY);
							}
							catch (Exception error)
							{

							}
						}
					}
				}
			}
		}

		private void Draw(object sender, EventArgs e)
		{
			DrawFrame();
		}

		private void DrawFrame()
		{
			if (animatedSprite != null)
			{
				try
				{
					int y = (CurrentFrame / FramesPerRow) * FrameHeight;
					animatedSprite = new CroppedBitmap(bitmap, new Int32Rect((CurrentFrame % FramesPerRow) * FrameWidth, y, FrameWidth, FrameHeight));
				}
				catch (Exception exception)
				{
				}

				if (AnimationState == ANIMATION_STATE_PLAY)
				{
					ChangeFrameInfo();
					GoToNextFrame();
				}
			}
		}

		private void ChangeFrameInfo()
		{
			int i = 0;

			FrameCanvas.Children.Clear();

			double maxX = FrameCanvas.ActualWidth / 2.0 - animatedSprite.PixelWidth / 2.0;
			double maxY = FrameCanvas.ActualHeight / 2.0 - animatedSprite.PixelHeight / 2.0;

			spriteImage.Source = animatedSprite;
			//spriteImage.Width = animatedSprite.PixelWidth;
			//spriteImage.Height = animatedSprite.PixelWidth;
			Canvas.SetLeft(spriteImage, maxX);
			Canvas.SetTop(spriteImage, maxY);

			FrameCanvas.Children.Add(spriteImage);

			StateAnimation anim = GetCurrentStateAnimation();
			if (anim != null)
			{
				ListOfHurtAndHitBoxes.Items.Clear();

				if (anim.HitOrHurtBoxListItems.Any() && anim.HitOrHurtBoxListItems.Count > CurrentFrame && anim.HitOrHurtBoxListItems[CurrentFrame].Any())
				{
					for (i = 0; i < anim.HitOrHurtBoxListItems[CurrentFrame].Count; i++)
					{
						ListOfHurtAndHitBoxes.Items.Add(anim.HitOrHurtBoxListItems[CurrentFrame][i]);
					}
				}

				if (anim.HitBoxPerFrame.Any() && anim.HitBoxPerFrame.Count > CurrentFrame && anim.HitBoxPerFrame[CurrentFrame].Any())
				{
					for (i = 0; i < anim.HitBoxPerFrame[CurrentFrame].Count; i++)
					{
						Canvas.SetLeft(anim.HitBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.HitBoxPerFrame[CurrentFrame][i].DrawRect.X);
						Canvas.SetTop(anim.HitBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.HitBoxPerFrame[CurrentFrame][i].DrawRect.Y);
						FrameCanvas.Children.Add(anim.HitBoxPerFrame[CurrentFrame][i].DrawRectangle);
					}
				}

				if (anim.HurtBoxPerFrame.Any() && anim.HurtBoxPerFrame.Count > CurrentFrame && anim.HurtBoxPerFrame[CurrentFrame].Any())
				{
					for (i = 0; i < anim.HurtBoxPerFrame[CurrentFrame].Count; i++)
					{
						Canvas.SetLeft(anim.HurtBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.HurtBoxPerFrame[CurrentFrame][i].DrawRect.X);
						Canvas.SetTop(anim.HurtBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.HurtBoxPerFrame[CurrentFrame][i].DrawRect.Y);
						FrameCanvas.Children.Add(anim.HurtBoxPerFrame[CurrentFrame][i].DrawRectangle);
					}
				}
			}
		}

		private void GoToNextFrame()
		{
			if (CurrentFrame + 1 >= NumberOfFrames)
			{
				CurrentFrame = 0;
			}
			else
			{
				CurrentFrame++;
			}
		}

		private void GoToPreviousFrame()
		{
			if (CurrentFrame - 1 < 0)
			{
				CurrentFrame = NumberOfFrames - 1;
			}
			else
			{
				CurrentFrame--;
			}
		}

		private void OpenAndShowImage(string filepath)
		{
			bitmap = new BitmapImage(new Uri(filepath, UriKind.Absolute));
			animatedSprite = new CroppedBitmap(bitmap, new Int32Rect(0, 0, 0, 0));

			FrameCanvas.Children.Clear();
			FrameCanvas.Children.Add(spriteImage);
		}

		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			SourceInitialized += (s, a) => WindowState = WindowState.Maximized;
			Show();

			MyBestiary = new Bestiary();

			updateTimer.Tick += Update;
			updateTimer.Interval = TimeSpan.FromSeconds(1.0 / 30.0);
			updateTimer.Start();

			drawTimer.Tick += Draw;
			drawTimer.Interval = TimeSpan.FromSeconds(1.0 / 30.0);
			drawTimer.Start();
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
			//SaveButton.IsEnabled = true;

			BestiaryNameLabel.Content = BestiaryNameTextBox.Text;

			BestiaryNameLabel.Visibility = Visibility.Visible;
			BestiaryNameTextBox.Visibility = Visibility.Hidden;
			ConfirmNameButton.Visibility = Visibility.Hidden;
			CancelNameButton.Visibility = Visibility.Hidden;

			//CreateUnitButton.IsEnabled = true;
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
			UnitState = item.Content.ToString();
			FrameCanvas.Children.Clear();

			List<StateAnimation> unitsStateAnimationList = GetCurrentStatesAnimations();

			if (unitsStateAnimationList != null)
			{
				ListOfStateAnimations.Items.Clear();

				for (int i = 0; i < unitsStateAnimationList.Count; i++)
				{
					ListBoxItem stateAnimationItem = new ListBoxItem();
					stateAnimationItem.Content = unitsStateAnimationList[i].FilePath;
					stateAnimationItem.Selected += OnAnimationSelected;
					stateAnimationItem.Unselected += OnAnimationUnselected;

					ListOfStateAnimations.Items.Add(stateAnimationItem);
				}

				SavePreviouslySelectedAnimationsInformation(previouslySelectedAnimationsName);
				SavePreviouslySelectedHitOrHurtBoxValues();

				if (ListOfStateAnimations.Items.Count > 0)
				{
					ListBoxItem stateAnimationItem = (ListBoxItem)ListOfStateAnimations.Items[0];
					stateAnimationItem.IsSelected = true;

					numberOfFramesTextBox.Text = unitsStateAnimationList[0].NumberOfFrames.ToString();
					frameWidthTextBox.Text = unitsStateAnimationList[0].FrameDimensionsX.ToString();
					frameHeightTextBox.Text = unitsStateAnimationList[0].FrameDimensionsY.ToString();
					framesPerRowTextBox.Text = unitsStateAnimationList[0].FramesPerRow.ToString();
					framesPerColumnTextBox.Text = unitsStateAnimationList[0].FramesPerColumn.ToString();
				}
			}

			//DeselectUnit();
			//SelectUnit(name);
		}

		private void OnStateUnselected(object sender, RoutedEventArgs e)
		{
		}

		private StateAnimation GetCurrentStateAnimation()
		{
			ListBoxItem item = (ListBoxItem)ListOfStateAnimations.SelectedItem;

			if (item != null)
			{
				string name = item.Content.ToString();

				return GetCurrentStateAnimation(name);
			}

			return null;
		}

		private StateAnimation GetCurrentStateAnimation(string name)
		{
			List<StateAnimation> unitsStateAnimationList = GetCurrentStatesAnimations();

			for (int i = 0; i < unitsStateAnimationList.Count; i++)
			{
				if (name == unitsStateAnimationList[i].FilePath)
				{
					return unitsStateAnimationList[i];
				}
			}

			return null;
		}

		private List<StateAnimation> GetCurrentStatesAnimations()
		{
			if (SelectedUnit != null)
			{
				List<StateAnimation> unitsStateAnimationList = new List<StateAnimation>();

				if (UnitState == "Idle")
				{
					unitsStateAnimationList = SelectedUnit.IdleAnimations;
				}
				else if (UnitState == "Walking")
				{
					unitsStateAnimationList = SelectedUnit.WalkingAnimations;
				}
				else if (UnitState == "Running")
				{
					unitsStateAnimationList = SelectedUnit.RunningAnimations;
				}
				else if (UnitState == "Dying")
				{
					unitsStateAnimationList = SelectedUnit.DyingAnimations;
				}
				else if (UnitState == "Dead")
				{
					unitsStateAnimationList = SelectedUnit.DeadAnimations;
				}
				else if (UnitState == "Attacking")
				{
					unitsStateAnimationList = SelectedUnit.AttackingAnimations;
				}
				else if (UnitState == "Blocking")
				{
					unitsStateAnimationList = SelectedUnit.BlockingAnimations;
				}
				else if (UnitState == "Hit Stun")
				{
					unitsStateAnimationList = SelectedUnit.HitStunAnimations;
				}
				else if (UnitState == "Jumping")
				{
					unitsStateAnimationList = SelectedUnit.JumpingAnimations;
				}
				else if (UnitState == "Jump Apex")
				{
					unitsStateAnimationList = SelectedUnit.JumpApexAnimations;
				}
				else if (UnitState == "Falling")
				{
					unitsStateAnimationList = SelectedUnit.FallingAnimations;
				}
				else if (UnitState == "Landing")
				{
					unitsStateAnimationList = SelectedUnit.LandingAnimations;
				}

				return unitsStateAnimationList;
			}

			return null;
		}

		private void OnAnimationSelected(object sender, RoutedEventArgs e)
		{
			SavePreviouslySelectedAnimationsInformation(previouslySelectedAnimationsName);
			SavePreviouslySelectedHitOrHurtBoxValues();

			ListBoxItem item = (ListBoxItem)sender;
			string name = item.Content.ToString();

			previouslySelectedAnimationsName = name;
			StateAnimation anim = GetCurrentStateAnimation(name);

			numberOfFramesTextBox.Text = anim.NumberOfFrames.ToString();
			frameWidthTextBox.Text = anim.FrameDimensionsX.ToString();
			frameHeightTextBox.Text = anim.FrameDimensionsY.ToString();
			framesPerRowTextBox.Text = anim.FramesPerRow.ToString();
			framesPerColumnTextBox.Text = anim.FramesPerColumn.ToString();

			ListOfHurtAndHitBoxes.Items.Clear();

			for (int i = 0; i < anim.HitOrHurtBoxListItems.Count; i++)
			{
				ListOfHurtAndHitBoxes.Items.Add(anim.HitOrHurtBoxListItems[i]);
			}

			OpenAndShowImage(anim.FilePath);
		}

		private void OnAnimationUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void SavePreviouslySelectedAnimationsInformation(string name)
		{
			StateAnimation anim = GetCurrentStateAnimation(name);

			if (anim != null)
			{
				int result = 0;

				if (int.TryParse(numberOfFramesTextBox.Text, out result))
				{
					anim.NumberOfFrames = result;
				}

				if (int.TryParse(frameWidthTextBox.Text, out result))
				{
					anim.FrameDimensionsX = result;
				}

				if (int.TryParse(frameHeightTextBox.Text, out result))
				{
					anim.FrameDimensionsY = result;
				}

				if (int.TryParse(framesPerRowTextBox.Text, out result))
				{
					anim.FramesPerRow = result;
				}

				if (int.TryParse(framesPerColumnTextBox.Text, out result))
				{
					anim.FramesPerColumn = result;
				}
			}
		}

		private void DeselectUnit()
		{
			ListOfStateAnimations.Items.Clear();
			ListOfHurtAndHitBoxes.Items.Clear();
			FrameCanvas.Children.Clear();
			numberOfFramesTextBox.Text = "";
			frameWidthTextBox.Text = "";
			frameHeightTextBox.Text = "";
			framesPerRowTextBox.Text = "";
			framesPerColumnTextBox.Text = "";
			framesPerSecondTextBox.Text = "";
			CurrentFrameTextBox.Text = "";
			xTextBox.Text = "";
			yTextBox.Text = "";
			wTextBox.Text = "";
			hTextBox.Text = "";

			//ListOfStates.IsEnabled = false;
		}

		private void SelectUnit(string name)
		{
			if (MyBestiary.DictOfUnits.ContainsKey(name))
			{
				SelectedUnit = MyBestiary.DictOfUnits[name];

				//ListOfStates.IsEnabled = true;
				ListBoxItem item = (ListBoxItem)ListOfStates.Items[0];
				item.IsSelected = true;

				UnitNameTextBox.Text = SelectedUnit.UnitName;
				HitPointsTextBox.Text = SelectedUnit.HitPoints.ToString();
				MovementSpeedTextBox.Text = SelectedUnit.MovementSpeed.ToString();
			}
		}

		void CreateUnit(object sender, RoutedEventArgs e)
		{
			//ListOfUnits.IsEnabled = true;

			string unitName = "Unit0";
			int unitIndex = 0;

			while (MyBestiary.DictOfUnits.ContainsKey(unitName))
			{
				unitIndex++;
				unitName = "Unit" + unitIndex;
			}

			Unit newUnit = new Unit();
			newUnit.UnitName = unitName;
			newUnit.HitPoints = 1;
			newUnit.MovementSpeed = 0.0f;

			UnitNameTextBox.Text = newUnit.UnitName.ToString();
			HitPointsTextBox.Text = newUnit.HitPoints.ToString();
			MovementSpeedTextBox.Text = newUnit.MovementSpeed.ToString();

			MyBestiary.DictOfUnits.Add(newUnit.UnitName, newUnit);

			ListBoxItem item = new ListBoxItem();
			item.Content = unitName;
			item.Selected += OnUnitSelected;
			item.Unselected += OnUnitUnselected;
			item.IsSelected = true;
			ListOfUnits.Items.Add(item);

			SelectUnit(newUnit.UnitName);
		}

		void DeleteUnit(object sender, RoutedEventArgs e)
		{
			if (ListOfUnits.Items.Count == 0)
			{
				//ListOfUnits.IsEnabled = false;
			}
		}

		public void CanvasDown(object sender, MouseButtonEventArgs e)
		{
			if (AnimationState == ANIMATION_STATE_PAUSED)
			{
				mouse_pressed_pos = e.GetPosition(FrameCanvas);

				SolidColorBrush BoxColor = HurtBoxColor;

				if (HitBoxCheckBox.IsChecked.HasValue && HitBoxCheckBox.IsChecked.Value)
				{
					BoxColor = HitBoxColor;
				}

				rect_getting_drawn = new Rectangle
				{
					Stroke = BoxColor,
					StrokeThickness = 1
				};
				Canvas.SetLeft(rect_getting_drawn, mouse_pressed_pos.X);
				Canvas.SetTop(rect_getting_drawn, mouse_pressed_pos.Y);
				FrameCanvas.Children.Add(rect_getting_drawn);
			}
		}

		public void CanvasMove(object sender, MouseEventArgs e)
		{
			if (rect_getting_drawn != null)
			{
				if (e.LeftButton == MouseButtonState.Released)
					return;

				var pos = e.GetPosition(FrameCanvas);

				var x = Math.Min(pos.X, mouse_pressed_pos.X);
				var y = Math.Min(pos.Y, mouse_pressed_pos.Y);

				var w = Math.Max(pos.X, mouse_pressed_pos.X) - x;
				var h = Math.Max(pos.Y, mouse_pressed_pos.Y) - y;

				rect_getting_drawn.Width = w;
				rect_getting_drawn.Height = h;

				Canvas.SetLeft(rect_getting_drawn, x);
				Canvas.SetTop(rect_getting_drawn, y);
			}
		}

		public void CanvasUp(object sender, MouseButtonEventArgs e)
		{
			if (rect_getting_drawn != null)
			{
				isDataDirty = true;

				//rectangles.Add(name, rect_getting_drawn);

				var pos = e.GetPosition(FrameCanvas);

				float top_left_x = Math.Min((float)mouse_pressed_pos.X, (float)pos.X);
				float top_left_y = Math.Min((float)mouse_pressed_pos.Y, (float)pos.Y);
				
				StateAnimation anim = GetCurrentStateAnimation();

				HitOrHurtBox newHitOrHurtBox = new HitOrHurtBox();
				newHitOrHurtBox.Box.X = top_left_x - (FrameCanvas.ActualWidth / 2.0f);
				newHitOrHurtBox.Box.Y = top_left_y - (FrameCanvas.ActualHeight / 2.0f);
				newHitOrHurtBox.Box.Width = rect_getting_drawn.Width;
				newHitOrHurtBox.Box.Height = rect_getting_drawn.Height;
				newHitOrHurtBox.DrawRect.X = top_left_x;
				newHitOrHurtBox.DrawRect.Y = top_left_y;
				newHitOrHurtBox.DrawRect.Width = rect_getting_drawn.Width;
				newHitOrHurtBox.DrawRect.Height = rect_getting_drawn.Height;
				newHitOrHurtBox.DrawRectangle = rect_getting_drawn;
				newHitOrHurtBox.Damage = 0;
				newHitOrHurtBox.KnockBackX = 0.0f;
				newHitOrHurtBox.KnockBackY = 0.0f;
				newHitOrHurtBox.Frame = CurrentFrame;

				AddNewHitOrHurtBox(anim, newHitOrHurtBox, CurrentFrame, HitBoxCheckBox.IsChecked.HasValue && HitBoxCheckBox.IsChecked.Value, true);

				//if (HitBoxCheckBox.IsChecked.HasValue && HitBoxCheckBox.IsChecked.Value)
				//{
				//	while (anim.HitBoxPerFrame.Count <= CurrentFrame)
				//	{
				//		anim.HitBoxPerFrame.Add(new List<HitOrHurtBox>());
				//	}
				//
				//	if (anim.HitBoxPerFrame.Count > CurrentFrame)
				//	{
				//		if (anim.HitBoxPerFrame[CurrentFrame] == null)
				//		{
				//			anim.HitBoxPerFrame[CurrentFrame] = new List<HitOrHurtBox>();
				//		}
				//
				//		newHitOrHurtBox.Name = "Hit " + anim.HitBoxPerFrame[CurrentFrame].Where(x => x.Name.Contains("Hit")).ToList().Count.ToString();
				//		anim.HitBoxPerFrame[CurrentFrame].Add(newHitOrHurtBox);
				//	}
				//}
				//else
				//{
				//	while (anim.HurtBoxPerFrame.Count <= CurrentFrame)
				//	{
				//		anim.HurtBoxPerFrame.Add(new List<HitOrHurtBox>());
				//	}
				//
				//	if (anim.HurtBoxPerFrame.Count > CurrentFrame)
				//	{
				//		if (anim.HurtBoxPerFrame[CurrentFrame] == null)
				//		{
				//			anim.HurtBoxPerFrame[CurrentFrame] = new List<HitOrHurtBox>();
				//		}
				//
				//		newHitOrHurtBox.Name = "Hurt " + anim.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
				//		anim.HurtBoxPerFrame[CurrentFrame].Add(newHitOrHurtBox);
				//	}
				//}
				//
				//ListBoxItem newListBoxItem = new ListBoxItem();
				//newListBoxItem.Content = newHitOrHurtBox.Name;
				//newListBoxItem.Selected += OnHitOrHurtBoxSelected;
				//newListBoxItem.Unselected += OnHitOrHurtBoxUnselected;
				//ListOfHurtAndHitBoxes.Items.Add(newListBoxItem);
				//
				////anim.HitOrHurtBoxListItems.Add(newListBoxItem);
				//while (anim.HitOrHurtBoxListItems.Count <= CurrentFrame)
				//{
				//	anim.HitOrHurtBoxListItems.Add(new List<ListBoxItem>());
				//}
				//
				//if (anim.HitOrHurtBoxListItems.Count > CurrentFrame)
				//{
				//	if (anim.HitOrHurtBoxListItems[CurrentFrame] == null)
				//	{
				//		anim.HitOrHurtBoxListItems[CurrentFrame] = new List<ListBoxItem>();
				//	}
				//
				//	//newHitOrHurtBox.Name = "Hurt " + anim.HitOrHurtBoxListItems[CurrentFrame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
				//	anim.HitOrHurtBoxListItems[CurrentFrame].Add(newListBoxItem);
				//}

				xTextBox.Text = newHitOrHurtBox.Box.X.ToString();
				yTextBox.Text = newHitOrHurtBox.Box.Y.ToString();
				wTextBox.Text = newHitOrHurtBox.Box.Width.ToString();
				hTextBox.Text = newHitOrHurtBox.Box.Height.ToString();
				DamageTextBox.Text = newHitOrHurtBox.Damage.ToString();
				KnockBackXTextBox.Text = newHitOrHurtBox.KnockBackX.ToString();
				KnockBackYTextBox.Text = newHitOrHurtBox.KnockBackY.ToString();

				//SerializedRectangle sRect = new SerializedRectangle();
				//sRect.type = "Rectangle";
				//sRect.name = name;
				//sRect.x = top_left_x;
				//sRect.y = top_left_y;
				//sRect.width = (float)rect_getting_drawn.Width;
				//sRect.height = (float)rect_getting_drawn.Height;

				//sRects.Add(sRect);

				//Select(name);

				rect_getting_drawn = null;
			}
		}

		private void AddNewHitOrHurtBox(StateAnimation anim, HitOrHurtBox newHitOrHurtBox, int frame, bool makingHitBox, bool addHitBoxToCurrentListBox)
		{
			if (makingHitBox)
			{
				while (anim.HitBoxPerFrame.Count <= frame)
				{
					anim.HitBoxPerFrame.Add(new List<HitOrHurtBox>());
				}

				if (anim.HitBoxPerFrame.Count > frame)
				{
					if (anim.HitBoxPerFrame[frame] == null)
					{
						anim.HitBoxPerFrame[frame] = new List<HitOrHurtBox>();
					}

					newHitOrHurtBox.Name = "Hit " + anim.HitBoxPerFrame[frame].Where(x => x.Name.Contains("Hit")).ToList().Count.ToString();
					anim.HitBoxPerFrame[frame].Add(newHitOrHurtBox);
				}
			}
			else
			{
				while (anim.HurtBoxPerFrame.Count <= frame)
				{
					anim.HurtBoxPerFrame.Add(new List<HitOrHurtBox>());
				}

				if (anim.HurtBoxPerFrame.Count > frame)
				{
					if (anim.HurtBoxPerFrame[frame] == null)
					{
						anim.HurtBoxPerFrame[frame] = new List<HitOrHurtBox>();
					}

					newHitOrHurtBox.Name = "Hurt " + anim.HurtBoxPerFrame[frame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
					anim.HurtBoxPerFrame[frame].Add(newHitOrHurtBox);
				}
			}

			ListBoxItem newListBoxItem = new ListBoxItem();
			newListBoxItem.Content = newHitOrHurtBox.Name;
			newListBoxItem.Selected += OnHitOrHurtBoxSelected;
			newListBoxItem.Unselected += OnHitOrHurtBoxUnselected;

			if (addHitBoxToCurrentListBox)
			{
				ListOfHurtAndHitBoxes.Items.Add(newListBoxItem);
			}

			//anim.HitOrHurtBoxListItems.Add(newListBoxItem);
			while (anim.HitOrHurtBoxListItems.Count <= frame)
			{
				anim.HitOrHurtBoxListItems.Add(new List<ListBoxItem>());
			}

			if (anim.HitOrHurtBoxListItems.Count > frame)
			{
				if (anim.HitOrHurtBoxListItems[frame] == null)
				{
					anim.HitOrHurtBoxListItems[frame] = new List<ListBoxItem>();
				}

				//newHitOrHurtBox.Name = "Hurt " + anim.HitOrHurtBoxListItems[frame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
				anim.HitOrHurtBoxListItems[frame].Add(newListBoxItem);
			}
		}

		private void SavePreviouslySelectedHitOrHurtBoxValues()
		{
			if (previouslySelectedHitOrHurtBox != null)
			{
				int result = 0;

				if (int.TryParse(xTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.Box.X = result;
				}

				if (int.TryParse(yTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.Box.Y = result;
				}

				if (int.TryParse(wTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.Box.Width = result;
				}

				if (int.TryParse(hTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.Box.Height = result;
				}

				if (int.TryParse(DamageTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.Damage = result;
				}

				if (int.TryParse(KnockBackXTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.KnockBackX = result;
				}

				if (int.TryParse(KnockBackYTextBox.Text, out result))
				{
					previouslySelectedHitOrHurtBox.KnockBackY = result;
				}
			}
		}

		private void OnHitOrHurtBoxSelected(object sender, RoutedEventArgs e)
		{
			SavePreviouslySelectedHitOrHurtBoxValues();

			ListBoxItem item = (ListBoxItem)sender;

			ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
			string selectedAnimationName = selectedAnimationItem.Content.ToString();
			StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

			List<List<HitOrHurtBox>> listOfBoxes = null;

			if (item.Content.ToString().Contains("Hit"))
			{
				listOfBoxes = selectedAnimation.HitBoxPerFrame;

				DamageTextBox.IsEnabled = true;
				KnockBackXTextBox.IsEnabled = true;
				KnockBackYTextBox.IsEnabled = true;
			}
			else
			{
				listOfBoxes = selectedAnimation.HurtBoxPerFrame;

				DamageTextBox.IsEnabled = false;
				KnockBackXTextBox.IsEnabled = false;
				KnockBackYTextBox.IsEnabled = false;
			}

			for (int i = 0; i < listOfBoxes[CurrentFrame].Count; i++)
			{
				HitOrHurtBox box = listOfBoxes[CurrentFrame][i];

				if (box.Name == item.Content.ToString())
				{
					xTextBox.Text = box.Box.X.ToString();
					yTextBox.Text = box.Box.Y.ToString();
					wTextBox.Text = box.Box.Width.ToString();
					hTextBox.Text = box.Box.Height.ToString();
					DamageTextBox.Text = box.Damage.ToString();
					KnockBackXTextBox.Text = box.KnockBackX.ToString();
					KnockBackYTextBox.Text = box.KnockBackY.ToString();

					break;
				}
			}
		}

		private void OnHitOrHurtBoxUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			SavePreviouslySelectedAnimationsInformation(previouslySelectedAnimationsName);
			SavePreviouslySelectedHitOrHurtBoxValues();

			MyBestiary.BestiaryName = BestiaryNameLabel.Content.ToString();
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
				MyBestiary = JsonConvert.DeserializeObject<Bestiary>(result);
				BestiaryNameLabel.Content = MyBestiary.BestiaryName;

				foreach (string key in MyBestiary.DictOfUnits.Keys)
				{
					Unit unit = MyBestiary.DictOfUnits[key];

					ListBoxItem item = new ListBoxItem();
					item.Content = unit.UnitName;
					item.Selected += OnUnitSelected;
					item.Unselected += OnUnitUnselected;
					//item.IsSelected = true;
					ListOfUnits.Items.Add(item);

					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.IdleAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.WalkingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.RunningAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.DyingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.DeadAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.AttackingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.BlockingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.HitStunAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.JumpingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.JumpApexAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.FallingAnimations);
					AddListBoxItemsForThisAnimationsHitAndHurtBoxes(unit.LandingAnimations);
				}

				FrameCanvas.Children.Clear();
				isDataDirty = false;
				//CreateUnitButton.IsEnabled = true;
			}
		}

		void AddListBoxItemsForThisAnimationsHitAndHurtBoxes(List<StateAnimation> anims)
		{
			for (int i = 0; i < anims.Count; i++)
			{
				for (int j = 0; j < anims[i].HitBoxPerFrame.Count; j++)
				{
					for (int k = 0; k < anims[i].HitBoxPerFrame[j].Count; k++)
					{
						AddListBoxItemsForThisAnimationsHitOrHurtBoxes(anims, i, j, k, true);
					}
				}

				for (int j = 0; j < anims[i].HurtBoxPerFrame.Count; j++)
				{
					for (int k = 0; k < anims[i].HurtBoxPerFrame[j].Count; k++)
					{
						AddListBoxItemsForThisAnimationsHitOrHurtBoxes(anims, i, j, k, false);
					}
				}
			}
		}

		void AddListBoxItemsForThisAnimationsHitOrHurtBoxes(List<StateAnimation> anims, int i, int j, int k, bool hitBoxes)
		{
			ListBoxItem newListBoxItem = new ListBoxItem();
			if (hitBoxes)
			{
				newListBoxItem.Content = anims[i].HitBoxPerFrame[j][k].Name;
			}
			else
			{
				newListBoxItem.Content = anims[i].HurtBoxPerFrame[j][k].Name;
			}
			newListBoxItem.Selected += OnHitOrHurtBoxSelected;
			newListBoxItem.Unselected += OnHitOrHurtBoxUnselected;
			ListOfHurtAndHitBoxes.Items.Add(newListBoxItem);

			while (anims[i].HitOrHurtBoxListItems.Count <= j)
			{
				anims[i].HitOrHurtBoxListItems.Add(new List<ListBoxItem>());
			}

			if (anims[i].HitOrHurtBoxListItems.Count > j)
			{
				if (anims[i].HitOrHurtBoxListItems[j] == null)
				{
					anims[i].HitOrHurtBoxListItems[j] = new List<ListBoxItem>();
				}

				anims[i].HitOrHurtBoxListItems[j].Add(newListBoxItem);
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

		private int NumberOfFrames
		{
			get
			{
				if (numberOfFramesTextBox.Text == string.Empty)
				{
					return 1;
				}

				int frames = 1;
				if (int.TryParse(numberOfFramesTextBox.Text, out frames))
				{
					return frames;
				}

				return 1;
			}
		}

		private int FramesPerSecond
		{
			get
			{
				if (framesPerSecondTextBox.Text == string.Empty)
				{
					return 1;
				}

				int frames = 1;
				if (int.TryParse(framesPerSecondTextBox.Text, out frames))
				{
					if (frames == 0)
					{
						return 1;
					}

					return frames;
				}

				return 1;
			}
		}

		private int FramesPerRow
		{
			get
			{
				if (framesPerRowTextBox.Text == string.Empty)
				{
					return 0;
				}

				int frames = 0;
				if (int.TryParse(framesPerRowTextBox.Text, out frames))
				{
					return frames;
				}

				return 0;
			}
		}

		private int FramesPerColumn
		{
			get
			{
				if (framesPerColumnTextBox.Text == string.Empty)
				{
					return 0;
				}

				int frames = 0;
				if (int.TryParse(framesPerColumnTextBox.Text, out frames))
				{
					return frames;
				}

				return 0;
			}
		}

		private int FrameWidth
		{
			get
			{
				if (frameWidthTextBox.Text == string.Empty)
				{
					return 1;
				}

				int frames = 1;
				if (int.TryParse(frameWidthTextBox.Text, out frames))
				{
					if (frames == 0)
					{
						return 1;
					}

					return frames;
				}

				return 1;
			}
		}

		private int FrameHeight
		{
			get
			{
				if (frameHeightTextBox.Text == string.Empty)
				{
					return 1;
				}

				int frames = 1;
				if (int.TryParse(frameHeightTextBox.Text, out frames))
				{
					if (frames == 0)
					{
						return 1;
					}

					return frames;
				}

				return 1;
			}
		}

		private int CurrentFrame
		{
			get
			{
				if (CurrentFrameTextBox.Text == string.Empty)
				{
					return 1;
				}

				int frames = 0;
				if (int.TryParse(CurrentFrameTextBox.Text, out frames))
				{
					//if (frames == 0)
					//{
					//	return 1;
					//}

					return frames;
				}

				return 1;
			}
			set
			{
				CurrentFrameTextBox.Text = value.ToString();
			}
		}

		//private void StateSelected(object sender, RoutedEventArgs e)
		//{
		//	ListBoxItem item = (ListBoxItem)sender;
		//	currentlySelectedState = item.Content.ToString();
		//}

		private void PreviousFrameButton_Click(object sender, RoutedEventArgs e)
		{
			GoToPreviousFrame();
			DrawFrame();
			ChangeFrameInfo();
		}

		private void NextFrameButton_Click(object sender, RoutedEventArgs e)
		{
			GoToNextFrame();
			DrawFrame();
			ChangeFrameInfo();
		}

		private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
		{
			if (AnimationState == ANIMATION_STATE_PLAY)
			{
				AnimationState = ANIMATION_STATE_PAUSED;
			}
			else
			{
				AnimationState = ANIMATION_STATE_PLAY;
			}
		}

		private void AddAnimationButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			//openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				OpenAndShowImage(openFileDialog.FileName);

				StateAnimation newStateAnimation = new StateAnimation();
				newStateAnimation.FilePath = openFileDialog.FileName;
				newStateAnimation.NumberOfFrames = 1;
				newStateAnimation.FrameDimensionsX = 10;
				newStateAnimation.FrameDimensionsY = 10;
				newStateAnimation.FramesPerRow = 1;
				newStateAnimation.FramesPerColumn = 1;
				newStateAnimation.SourceDimensionsX = (int)animatedSprite.PixelWidth;
				newStateAnimation.SourceDimensionsX = (int)animatedSprite.PixelHeight;

				AddStateAnimationToSelectedUnitsState(newStateAnimation);

				ListBoxItem item = new ListBoxItem();
				item.Content = newStateAnimation.FilePath;
				item.Selected += OnAnimationSelected;
				item.Unselected += OnAnimationUnselected;
				item.IsSelected = true;

				ListOfStateAnimations.Items.Add(item);
				//ListOfStateAnimations.IsEnabled = true;
			}
		}

		private void AddStateAnimationToSelectedUnitsState(StateAnimation stateAnimation)
		{
			List<StateAnimation> unitsStateAnimationList = GetCurrentStatesAnimations();
			unitsStateAnimationList.Add(stateAnimation);
		}

		private void HitBoxCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			HurtBoxCheckBox.IsChecked = false;
		}

		private void HurtBoxCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			HitBoxCheckBox.IsChecked = false;
		}

		private void DeleteAnimationButton_Click(object sender, RoutedEventArgs e)
		{
			if (ListOfStateAnimations.SelectedItem != null)
			{
				string sCaption = "Delete Animation";
				string sMessageBoxText = "This will delete the currently selected\nanimation and cannot be undone.\n\nDo you wish to continue?";

				MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
				MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

				MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

				switch (rsltMessageBox)
				{
					case MessageBoxResult.Yes:
						ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
						string selectedAnimationName = selectedAnimationItem.Content.ToString();
						List<StateAnimation> unitsStateAnimationList = GetCurrentStatesAnimations();

						for (int i = 0; i < unitsStateAnimationList.Count; i++)
						{
							if (selectedAnimationName == unitsStateAnimationList[i].FilePath)
							{
								unitsStateAnimationList.RemoveAt(i);
								break;
							}
						}

						animatedSprite = null;

						ListOfStateAnimations.Items.Remove(selectedAnimationItem);

						break;
					case MessageBoxResult.No:
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
		}

		private void DeleteHitOrHurtBoxButton_Click(object sender, RoutedEventArgs e)
		{
			if (ListOfHurtAndHitBoxes.SelectedItem != null)
			{
				string sCaption = "Delete Hit/Hurt Box";
				string sMessageBoxText = "This will delete the currently selected\nhit/hurt box and cannot be undone.\n\nDo you wish to continue?";

				MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
				MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

				MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

				switch (rsltMessageBox)
				{
					case MessageBoxResult.Yes:
						ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
						string selectedAnimationName = selectedAnimationItem.Content.ToString();
						StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

						ListBoxItem selectedHitOrHurtBoxItem = (ListBoxItem)ListOfHurtAndHitBoxes.SelectedItem;
						string selectedHitOrHurtBoxName = selectedHitOrHurtBoxItem.Content.ToString();

						bool breakOutOfForLoops = false;

						if (selectedHitOrHurtBoxName.Contains("Hit"))
						{
							for (int i = 0; i < selectedAnimation.HitBoxPerFrame.Count; i++)
							{
								for (int box = 0; box < selectedAnimation.HitBoxPerFrame[i].Count; box++)
								{
									if (selectedHitOrHurtBoxName == selectedAnimation.HitBoxPerFrame[i][box].Name)
									{
										FrameCanvas.Children.Remove(selectedAnimation.HitBoxPerFrame[i][box].DrawRectangle);
										selectedAnimation.HitBoxPerFrame[i][box].DrawRectangle = null;
										selectedAnimation.HitBoxPerFrame[i].RemoveAt(box);
										breakOutOfForLoops = true;
										break;
									}
								}

								if (breakOutOfForLoops)
								{
									break;
								}
							}
						}
						else // Hurt
						{
							for (int i = 0; i < selectedAnimation.HurtBoxPerFrame.Count; i++)
							{
								for (int box = 0; box < selectedAnimation.HurtBoxPerFrame[i].Count; box++)
								{
									if (selectedHitOrHurtBoxName == selectedAnimation.HurtBoxPerFrame[i][box].Name)
									{
										FrameCanvas.Children.Remove(selectedAnimation.HurtBoxPerFrame[i][box].DrawRectangle);
										selectedAnimation.HurtBoxPerFrame[i][box].DrawRectangle = null;
										selectedAnimation.HurtBoxPerFrame[i].RemoveAt(box);
										breakOutOfForLoops = true;
										break;
									}
								}

								if (breakOutOfForLoops)
								{
									break;
								}
							}
						}

						breakOutOfForLoops = false;
						for (int i = 0; i < selectedAnimation.HitOrHurtBoxListItems.Count; i++)
						{
							for (int box = 0; box < selectedAnimation.HitOrHurtBoxListItems[i].Count; box++)
							{
								if (selectedHitOrHurtBoxName == selectedAnimation.HitOrHurtBoxListItems[i][box].Content.ToString())
								{
									selectedAnimation.HitOrHurtBoxListItems[i].RemoveAt(box);
									breakOutOfForLoops = true;
									break;
								}
							}

							if (breakOutOfForLoops)
							{
								break;
							}
						}

						ListOfHurtAndHitBoxes.Items.Remove(selectedHitOrHurtBoxItem);

						break;
					case MessageBoxResult.No:
						break;
					case MessageBoxResult.Cancel:
						break;
				}
			}
		}

		private void CopyBoxToNextFrameButton_Click(object sender, RoutedEventArgs e)
		{
			if (ListOfHurtAndHitBoxes.SelectedItem != null)
			{
				ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
				string selectedAnimationName = selectedAnimationItem.Content.ToString();
				StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

				if (CurrentFrame + 1 < selectedAnimation.NumberOfFrames)
				{
					ListBoxItem selectedHitOrHurtBoxItem = (ListBoxItem)ListOfHurtAndHitBoxes.SelectedItem;
					string selectedHitOrHurtBoxName = selectedHitOrHurtBoxItem.Content.ToString();
					HitOrHurtBox origHitOrHurtBox;
					bool isHitBox = selectedHitOrHurtBoxName.Contains("Hit");

					if (isHitBox)
					{
						origHitOrHurtBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedHitOrHurtBoxName).First();
					}
					else
					{
						origHitOrHurtBox = selectedAnimation.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedHitOrHurtBoxName).First();
					}

					if (origHitOrHurtBox != null)
					{
						HitOrHurtBox newHitOrHurtBox = new HitOrHurtBox();
						newHitOrHurtBox.Box.X = origHitOrHurtBox.Box.X;
						newHitOrHurtBox.Box.Y = origHitOrHurtBox.Box.Y;
						newHitOrHurtBox.Box.Width = origHitOrHurtBox.Box.Width;
						newHitOrHurtBox.Box.Height = origHitOrHurtBox.Box.Height;
						newHitOrHurtBox.DrawRect.X = origHitOrHurtBox.DrawRect.X;
						newHitOrHurtBox.DrawRect.Y = origHitOrHurtBox.DrawRect.Y;
						newHitOrHurtBox.DrawRect.Width = origHitOrHurtBox.DrawRect.Width;
						newHitOrHurtBox.DrawRect.Height = origHitOrHurtBox.DrawRect.Height;
						newHitOrHurtBox.DrawRectangle = origHitOrHurtBox.DrawRectangle;
						newHitOrHurtBox.Damage = origHitOrHurtBox.Damage;
						newHitOrHurtBox.KnockBackX = origHitOrHurtBox.KnockBackX;
						newHitOrHurtBox.KnockBackY = origHitOrHurtBox.KnockBackY;
						newHitOrHurtBox.Frame = CurrentFrame + 1;

						AddNewHitOrHurtBox(selectedAnimation, newHitOrHurtBox, CurrentFrame + 1, isHitBox, false);
					}
				}
			}
		}

		private void SelectivelyHandleMouseButton(object sender, MouseButtonEventArgs e)
		{
			var textbox = (sender as TextBox);
			if (textbox != null && !textbox.IsKeyboardFocusWithin)
			{
				if (e.OriginalSource.GetType().Name == "TextBoxView")
				{
					e.Handled = true;
					textbox.Focus();
				}
			}
		}

		private void SelectAllText(object sender, RoutedEventArgs e)
		{
			var textBox = e.OriginalSource as TextBox;
			if (textBox != null)
				textBox.SelectAll();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Right)
			{
				StateAnimation anim = GetCurrentStateAnimation();
				if (anim != null)
				{
					NextFrameButton_Click(null, null);
				}
			}

			if (e.Key == Key.Left)
			{
				StateAnimation anim = GetCurrentStateAnimation();
				if (anim != null)
				{
					PreviousFrameButton_Click(null, null);
				}
			}
		}
	}

	public class Bestiary
	{
		public string BestiaryName = "";
		public Dictionary<string, Unit> DictOfUnits = new Dictionary<string, Unit>();
		//public List<Unit> Units;

		public Bestiary()
		{
		}
	}

	public class Unit
	{
		public string UnitName = "";
		public int HitPoints = 0;
		public float MovementSpeed = 0.0f;
		public List<StateAnimation> IdleAnimations = new List<StateAnimation>();
		public List<StateAnimation> WalkingAnimations = new List<StateAnimation>();
		public List<StateAnimation> RunningAnimations = new List<StateAnimation>();
		public List<StateAnimation> DyingAnimations = new List<StateAnimation>();
		public List<StateAnimation> DeadAnimations = new List<StateAnimation>();
		public List<StateAnimation> AttackingAnimations = new List<StateAnimation>();
		public List<StateAnimation> BlockingAnimations = new List<StateAnimation>();
		public List<StateAnimation> HitStunAnimations = new List<StateAnimation>();
		public List<StateAnimation> JumpingAnimations = new List<StateAnimation>();
		public List<StateAnimation> JumpApexAnimations = new List<StateAnimation>();
		public List<StateAnimation> FallingAnimations = new List<StateAnimation>();
		public List<StateAnimation> LandingAnimations = new List<StateAnimation>();

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
		public int FramesPerRow = 0;
		public int FramesPerColumn = 0;
		public List<List<HitOrHurtBox>> HurtBoxPerFrame = new List<List<HitOrHurtBox>>();
		public List<List<HitOrHurtBox>> HitBoxPerFrame = new List<List<HitOrHurtBox>>();
		[JsonIgnore]
		public List<List<ListBoxItem>> HitOrHurtBoxListItems = new List<List<ListBoxItem>>();

		public StateAnimation()
		{
		}
	}

	public class HitOrHurtBox
	{
		public string Name = "";
		public Rect Box = new Rect();
		public Rect DrawRect = new Rect();
		public int Frame = 0;
		[JsonIgnore]
		private Rectangle _drawRectangle;
		[JsonIgnore]
		public Rectangle DrawRectangle
		{
			get
			{
				if (_drawRectangle == null)
				{
					_drawRectangle = new Rectangle();
					_drawRectangle.Width = DrawRect.Width;
					_drawRectangle.Height = DrawRect.Height;
					_drawRectangle.Stroke = Name.Contains("Hit") ? Brushes.Red : Brushes.Green;
					_drawRectangle.StrokeThickness = 1;
				}

				return _drawRectangle;
			}
			set
			{
				_drawRectangle = value;
			}
		}
		public int Damage = 0;
		public float KnockBackX = 0.0f;
		public float KnockBackY = 0.0f;

		public HitOrHurtBox()
		{
		}
	}
}
