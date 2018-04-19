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

		AnimationInformationBox previouslySelectedAnimationInformationBox = null;
		
		SolidColorBrush HurtBoxColor = Brushes.Green;
		SolidColorBrush HitBoxColor = Brushes.Red;
		SolidColorBrush InteractionCircleColor = Brushes.Yellow;
		SolidColorBrush ProjectileSpawnBoxColor = Brushes.Violet;

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

			if (PoiseTextBox.IsFocused)
			{
				ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
				if (selectedAnimationItem != null)
				{
					string selectedAnimationName = selectedAnimationItem.Content.ToString();
					StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

					if (selectedAnimation != null)
					{
						int poise = 0;

						int.TryParse(PoiseTextBox.Text, out poise);

						selectedAnimation.Poise = poise;
					}
				}
			}

			if (frameWidthTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						int result = 0;
						int.TryParse(frameWidthTextBox.Text, out result);
						
						anim.FrameDimensionsX = result;
					}
				}
			}

			if (frameHeightTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						int result = 0;
						int.TryParse(frameHeightTextBox.Text, out result);

						anim.FrameDimensionsY = result;
					}
				}
			}

			if (numberOfFramesTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						int result = 0;
						int.TryParse(numberOfFramesTextBox.Text, out result);

						anim.NumberOfFrames = result;
					}
				}
			}

			if (framesPerRowTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						int result = 0;
						int.TryParse(framesPerRowTextBox.Text, out result);

						anim.FramesPerRow = result;
					}
				}
			}

			if (framesPerColumnTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						int result = 0;
						int.TryParse(framesPerColumnTextBox.Text, out result);

						anim.FramesPerColumn = result;
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

			if (InteractionRadiusTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					float result = 0.0f;
					float.TryParse(InteractionRadiusTextBox.Text, out result);

					SelectedUnit.InteractionRadius = result;
				}
			}

			if (SwingingSoundFileTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						while (anim.AttackSoundFilePerFrame.Count <= CurrentFrame)
						{
							anim.AttackSoundFilePerFrame.Add("");
						}

						anim.AttackSoundFilePerFrame[CurrentFrame] = SwingingSoundFileTextBox.Text;
					}
				}
			}

			if (RightFootSoundFileTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						while (anim.RightFootSoundFilePerFrame.Count <= CurrentFrame)
						{
							anim.RightFootSoundFilePerFrame.Add("");
						}

						anim.RightFootSoundFilePerFrame[CurrentFrame] = RightFootSoundFileTextBox.Text;
					}
				}
			}

			if (LeftFootSoundFileTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						while (anim.LeftFootSoundFilePerFrame.Count <= CurrentFrame)
						{
							anim.LeftFootSoundFilePerFrame.Add("");
						}

						anim.LeftFootSoundFilePerFrame[CurrentFrame] = LeftFootSoundFileTextBox.Text;
					}
				}
			}

			if (ThrowProjectileSoundFileTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						while (anim.ThrowProjectileSoundFilePerFrame.Count <= CurrentFrame)
						{
							anim.ThrowProjectileSoundFilePerFrame.Add("");
						}

						anim.ThrowProjectileSoundFilePerFrame[CurrentFrame] = ThrowProjectileSoundFileTextBox.Text;
					}
				}
			}

			if (ProjectileHitSoundFileTextBox.IsFocused)
			{
				if (SelectedUnit != null)
				{
					StateAnimation anim = GetCurrentStateAnimation();
					if (anim != null)
					{
						while (anim.ProjectileHitSoundFilePerFrame.Count <= CurrentFrame)
						{
							anim.ProjectileHitSoundFilePerFrame.Add("");
						}

						anim.ProjectileHitSoundFilePerFrame[CurrentFrame] = ProjectileHitSoundFileTextBox.Text;
					}
				}
			}

			if ((PoiseTextBox.IsFocused || DamageTextBox.IsFocused || KnockBackXTextBox.IsFocused || KnockBackYTextBox.IsFocused || ProjectileSpeedXTextBox.IsFocused || ProjectileSpeedYTextBox.IsFocused || HitStunFramesTextBox.IsFocused) && SelectedUnit != null)
			{
				StateAnimation selectedAnimation = GetCurrentStateAnimation();
				if (selectedAnimation != null)
				{
					if (ListOfAnimationInformationBoxes.SelectedItem != null)
					{
						ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
						string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();
						AnimationInformationBox selectedAnimationInformationBox;

						if (selectedAnimationInformationBoxName.Contains("Hit"))
						{
							selectedAnimationInformationBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
						}
						else if (selectedAnimationInformationBoxName.Contains("Hurt"))
						{
							selectedAnimationInformationBox = selectedAnimation.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
						}
						else// if (selectedAnimationInformationBoxName.Contains("Projectile Spawn"))
						{
							selectedAnimationInformationBox = selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
						}

						if (selectedAnimationInformationBox != null)
						{
							try
							{
								int.TryParse(DamageTextBox.Text, out selectedAnimationInformationBox.Damage);
								int.TryParse(PoiseTextBox.Text, out selectedAnimation.Poise);
								float.TryParse(KnockBackXTextBox.Text, out selectedAnimationInformationBox.KnockBackX);
								float.TryParse(KnockBackYTextBox.Text, out selectedAnimationInformationBox.KnockBackY);
								float.TryParse(ProjectileSpeedXTextBox.Text, out selectedAnimationInformationBox.ProjectileSpeedX);
								float.TryParse(ProjectileSpeedYTextBox.Text, out selectedAnimationInformationBox.ProjectileSpeedY);
								int.TryParse(HitStunFramesTextBox.Text, out selectedAnimationInformationBox.HitStunFrames);
							}
							catch (Exception exception)
							{
								string ignore_exception = exception.Message;
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
					string ignore_exception = exception.Message;
				}

				if (AnimationState == ANIMATION_STATE_PLAY)
				{
					GoToNextFrame();
					ChangeFrameInfo();
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
			Canvas.SetLeft(spriteImage, maxX);
			Canvas.SetTop(spriteImage, maxY);

			FrameCanvas.Children.Add(spriteImage);

			StateAnimation anim = GetCurrentStateAnimation();
			if (anim != null)
			{
				ListOfAnimationInformationBoxes.Items.Clear();

				if (anim.AnimationInformationBoxListItems.Any() && anim.AnimationInformationBoxListItems.Count > CurrentFrame && anim.AnimationInformationBoxListItems[CurrentFrame].Any())
				{
					for (i = 0; i < anim.AnimationInformationBoxListItems[CurrentFrame].Count; i++)
					{
						ListOfAnimationInformationBoxes.Items.Add(anim.AnimationInformationBoxListItems[CurrentFrame][i]);
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

				if (anim.ProjectileSpawnBoxPerFrame.Any() && anim.ProjectileSpawnBoxPerFrame.Count > CurrentFrame && anim.ProjectileSpawnBoxPerFrame[CurrentFrame].Any())
				{
					for (i = 0; i < anim.ProjectileSpawnBoxPerFrame[CurrentFrame].Count; i++)
					{
						Canvas.SetLeft(anim.ProjectileSpawnBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.ProjectileSpawnBoxPerFrame[CurrentFrame][i].DrawRect.X);
						Canvas.SetTop(anim.ProjectileSpawnBoxPerFrame[CurrentFrame][i].DrawRectangle, anim.ProjectileSpawnBoxPerFrame[CurrentFrame][i].DrawRect.Y);
						FrameCanvas.Children.Add(anim.ProjectileSpawnBoxPerFrame[CurrentFrame][i].DrawRectangle);
					}
				}

				if (anim.AttackSoundFilePerFrame.Any() && anim.AttackSoundFilePerFrame.Count > CurrentFrame)
				{
					SwingingSoundFileTextBox.Text = anim.AttackSoundFilePerFrame[CurrentFrame];
				}
				else
				{
					SwingingSoundFileTextBox.Text = "";
				}

				if (anim.RightFootSoundFilePerFrame.Any() && anim.RightFootSoundFilePerFrame.Count > CurrentFrame)
				{
					RightFootSoundFileTextBox.Text = anim.RightFootSoundFilePerFrame[CurrentFrame];
				}
				else
				{
					RightFootSoundFileTextBox.Text = "";
				}

				if (anim.LeftFootSoundFilePerFrame.Any() && anim.LeftFootSoundFilePerFrame.Count > CurrentFrame)
				{
					LeftFootSoundFileTextBox.Text = anim.LeftFootSoundFilePerFrame[CurrentFrame];
				}
				else
				{
					LeftFootSoundFileTextBox.Text = "";
				}

				if (anim.ThrowProjectileSoundFilePerFrame.Any() && anim.ThrowProjectileSoundFilePerFrame.Count > CurrentFrame)
				{
					ThrowProjectileSoundFileTextBox.Text = anim.ThrowProjectileSoundFilePerFrame[CurrentFrame];
				}
				else
				{
					ThrowProjectileSoundFileTextBox.Text = "";
				}

				if (anim.ProjectileHitSoundFilePerFrame.Any() && anim.ProjectileHitSoundFilePerFrame.Count > CurrentFrame)
				{
					ProjectileHitSoundFileTextBox.Text = anim.ProjectileHitSoundFilePerFrame[CurrentFrame];
				}
				else
				{
					ProjectileHitSoundFileTextBox.Text = "";
				}
			}

			if (SelectedUnit != null && SelectedUnit.InteractionRadius > 0.0f)
			{
				Ellipse circle = new Ellipse();
				circle.StrokeThickness = 2;
				circle.Stroke = InteractionCircleColor;
				circle.Height = SelectedUnit.InteractionRadius * 2.0f;
				circle.Width = SelectedUnit.InteractionRadius * 2.0f;

				Canvas.SetLeft(circle, (FrameCanvas.ActualWidth / 2.0f) - SelectedUnit.InteractionRadius);
				Canvas.SetTop(circle, (FrameCanvas.ActualHeight / 2.0f) - SelectedUnit.InteractionRadius);

				FrameCanvas.Children.Add(circle);
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
				SavepreviouslySelectedAnimationInformationBoxValues();

				if (ListOfStateAnimations.Items.Count > 0)
				{
					ListBoxItem stateAnimationItem = (ListBoxItem)ListOfStateAnimations.Items[0];
					stateAnimationItem.IsSelected = true;

					numberOfFramesTextBox.Text = unitsStateAnimationList[0].NumberOfFrames.ToString();
					PoiseTextBox.Text = unitsStateAnimationList[0].Poise.ToString();
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
				else if (UnitState == "Talking")
				{
					unitsStateAnimationList = SelectedUnit.TalkingAnimations;
				}
				else if (UnitState == "Projectile Active")
				{
					unitsStateAnimationList = SelectedUnit.ProjectileActiveAnimations;
				}
				else if (UnitState == "Projectile Hit")
				{
					unitsStateAnimationList = SelectedUnit.ProjectileHitAnimations;
				}

				return unitsStateAnimationList;
			}

			return null;
		}

		private void OnAnimationSelected(object sender, RoutedEventArgs e)
		{
			SavePreviouslySelectedAnimationsInformation(previouslySelectedAnimationsName);
			SavepreviouslySelectedAnimationInformationBoxValues();

			ListBoxItem item = (ListBoxItem)sender;
			string name = item.Content.ToString();

			previouslySelectedAnimationsName = name;
			StateAnimation anim = GetCurrentStateAnimation(name);

			numberOfFramesTextBox.Text = anim.NumberOfFrames.ToString();
			PoiseTextBox.Text = anim.Poise.ToString();
			frameWidthTextBox.Text = anim.FrameDimensionsX.ToString();
			frameHeightTextBox.Text = anim.FrameDimensionsY.ToString();
			framesPerRowTextBox.Text = anim.FramesPerRow.ToString();
			framesPerColumnTextBox.Text = anim.FramesPerColumn.ToString();

			ListOfAnimationInformationBoxes.Items.Clear();

			for (int i = 0; i < anim.AnimationInformationBoxListItems.Count; i++)
			{
				ListOfAnimationInformationBoxes.Items.Add(anim.AnimationInformationBoxListItems[i]);
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

				if (int.TryParse(PoiseTextBox.Text, out result))
				{
					anim.Poise = result;
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
			ListOfAnimationInformationBoxes.Items.Clear();
			FrameCanvas.Children.Clear();
			numberOfFramesTextBox.Text = "";
			PoiseTextBox.Text = "";
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
				InteractionRadiusTextBox.Text = SelectedUnit.InteractionRadius.ToString();
				FlyingCheckBox.IsChecked = SelectedUnit.Flying;

				JumpingSoundFileTextBox.Text = SelectedUnit.JumpingSoundFile;
				LandingSoundFileTextBox.Text = SelectedUnit.LandingSoundFile;

				ListOfGettingHitSoundFiles.Items.Clear();
				for (int i = 0; i < SelectedUnit.GettingHitSoundFiles.Count; i++)
				{
					ListBoxItem new_item = new ListBoxItem();
					new_item.Content = SelectedUnit.GettingHitSoundFiles[i];

					ListOfGettingHitSoundFiles.Items.Add(new_item);
				}
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
			newUnit.InteractionRadius = 0.0f;

			UnitNameTextBox.Text = newUnit.UnitName.ToString();
			HitPointsTextBox.Text = newUnit.HitPoints.ToString();
			MovementSpeedTextBox.Text = newUnit.MovementSpeed.ToString();
			InteractionRadiusTextBox.Text = newUnit.InteractionRadius.ToString();

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
				else if (ProjectileSpawnCheckBox.IsChecked.HasValue && ProjectileSpawnCheckBox.IsChecked.Value)
				{
					BoxColor = ProjectileSpawnBoxColor;
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

				AnimationInformationBox newAnimationInformationBox = new AnimationInformationBox();
				newAnimationInformationBox.Box.X = top_left_x - (FrameCanvas.ActualWidth / 2.0f);
				newAnimationInformationBox.Box.Y = top_left_y - (FrameCanvas.ActualHeight / 2.0f);
				newAnimationInformationBox.Box.Width = rect_getting_drawn.Width;
				newAnimationInformationBox.Box.Height = rect_getting_drawn.Height;
				newAnimationInformationBox.DrawRect.X = top_left_x;
				newAnimationInformationBox.DrawRect.Y = top_left_y;
				newAnimationInformationBox.DrawRect.Width = rect_getting_drawn.Width;
				newAnimationInformationBox.DrawRect.Height = rect_getting_drawn.Height;
				newAnimationInformationBox.DrawRectangle = rect_getting_drawn;
				newAnimationInformationBox.Damage = 0;
				newAnimationInformationBox.KnockBackX = 0.0f;
				newAnimationInformationBox.KnockBackY = 0.0f;
				newAnimationInformationBox.HitStunFrames = 0;
				newAnimationInformationBox.Frame = CurrentFrame;
				newAnimationInformationBox.PopUp = false;
				newAnimationInformationBox.ArcProjectile = false;

				string box_type = "";
				if (HitBoxCheckBox.IsChecked.HasValue && HitBoxCheckBox.IsChecked.Value)
				{
					box_type = "Hit";
				}
				else if (HurtBoxCheckBox.IsChecked.HasValue && HurtBoxCheckBox.IsChecked.Value)
				{
					box_type = "Hurt";
				}
				else if (ProjectileSpawnCheckBox.IsChecked.HasValue && ProjectileSpawnCheckBox.IsChecked.Value)
				{
					box_type = "Projectile Spawn";
				}

				AddNewAnimationInformationBox(anim, newAnimationInformationBox, CurrentFrame, box_type, true);

				xTextBox.Text = newAnimationInformationBox.Box.X.ToString();
				yTextBox.Text = newAnimationInformationBox.Box.Y.ToString();
				wTextBox.Text = newAnimationInformationBox.Box.Width.ToString();
				hTextBox.Text = newAnimationInformationBox.Box.Height.ToString();
				DamageTextBox.Text = newAnimationInformationBox.Damage.ToString();
				PoiseTextBox.Text = anim.Poise.ToString();
				KnockBackXTextBox.Text = newAnimationInformationBox.KnockBackX.ToString();
				KnockBackYTextBox.Text = newAnimationInformationBox.KnockBackY.ToString();
				ProjectileSpeedXTextBox.Text = newAnimationInformationBox.ProjectileSpeedX.ToString();
				ProjectileSpeedYTextBox.Text = newAnimationInformationBox.ProjectileSpeedY.ToString();
				HitStunFramesTextBox.Text = newAnimationInformationBox.HitStunFrames.ToString();
				PopUpCheckBox.IsChecked = newAnimationInformationBox.PopUp;
				ArcProjectileCheckBox.IsChecked = newAnimationInformationBox.ArcProjectile;

				rect_getting_drawn = null;
			}
		}

		private void AddNewAnimationInformationBox(StateAnimation anim, AnimationInformationBox newAnimationInformationBox, int frame, string name, bool addHitBoxToCurrentListBox)
		{
			if (name.Contains("Hit"))
			{
				while (anim.HitBoxPerFrame.Count <= frame)
				{
					anim.HitBoxPerFrame.Add(new List<AnimationInformationBox>());
				}
				
				if (anim.HitBoxPerFrame.Count > frame)
				{
					if (anim.HitBoxPerFrame[frame] == null)
					{
						anim.HitBoxPerFrame[frame] = new List<AnimationInformationBox>();
					}

					newAnimationInformationBox.Name = "Hit " + anim.HitBoxPerFrame[frame].Where(x => x.Name.Contains("Hit")).ToList().Count.ToString();
					anim.HitBoxPerFrame[frame].Add(newAnimationInformationBox);
				}
			}
			else if (name.Contains("Hurt"))
			{
				while (anim.HurtBoxPerFrame.Count <= frame)
				{
					anim.HurtBoxPerFrame.Add(new List<AnimationInformationBox>());
				}
				
				if (anim.HurtBoxPerFrame.Count > frame)
				{
					if (anim.HurtBoxPerFrame[frame] == null)
					{
						anim.HurtBoxPerFrame[frame] = new List<AnimationInformationBox>();
					}

					newAnimationInformationBox.Name = "Hurt " + anim.HurtBoxPerFrame[frame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
					anim.HurtBoxPerFrame[frame].Add(newAnimationInformationBox);
				}
			}
			else if (name.Contains("Projectile Spawn"))
			{
				while (anim.ProjectileSpawnBoxPerFrame.Count <= frame)
				{
					anim.ProjectileSpawnBoxPerFrame.Add(new List<AnimationInformationBox>());
				}
				
				if (anim.ProjectileSpawnBoxPerFrame.Count > frame)
				{
					if (anim.ProjectileSpawnBoxPerFrame[frame] == null)
					{
						anim.ProjectileSpawnBoxPerFrame[frame] = new List<AnimationInformationBox>();
					}

					newAnimationInformationBox.Name = "Projectile Spawn " + anim.ProjectileSpawnBoxPerFrame[frame].Where(x => x.Name.Contains("Projectile Spawn")).ToList().Count.ToString();
					anim.ProjectileSpawnBoxPerFrame[frame].Add(newAnimationInformationBox);
				}
			}

			ListBoxItem newListBoxItem = new ListBoxItem();
			newListBoxItem.Content = newAnimationInformationBox.Name;
			newListBoxItem.Selected += OnAnimationInformationBoxSelected;
			newListBoxItem.Unselected += OnAnimationInformationBoxUnselected;

			if (addHitBoxToCurrentListBox)
			{
				ListOfAnimationInformationBoxes.Items.Add(newListBoxItem);
			}

			//anim.AnimationInformationBoxListItems.Add(newListBoxItem);
			while (anim.AnimationInformationBoxListItems.Count <= frame)
			{
				anim.AnimationInformationBoxListItems.Add(new List<ListBoxItem>());
			}

			if (anim.AnimationInformationBoxListItems.Count > frame)
			{
				if (anim.AnimationInformationBoxListItems[frame] == null)
				{
					anim.AnimationInformationBoxListItems[frame] = new List<ListBoxItem>();
				}

				//newHitOrHurtBox.Name = "Hurt " + anim.AnimationInformationBoxListItems[frame].Where(x => x.Name.Contains("Hurt")).ToList().Count.ToString();
				anim.AnimationInformationBoxListItems[frame].Add(newListBoxItem);
			}
		}

		private void SavepreviouslySelectedAnimationInformationBoxValues()
		{
			if (previouslySelectedAnimationInformationBox != null)
			{
				int result = 0;

				if (int.TryParse(xTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.Box.X = result;
				}

				if (int.TryParse(yTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.Box.Y = result;
				}

				if (int.TryParse(wTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.Box.Width = result;
				}

				if (int.TryParse(hTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.Box.Height = result;
				}

				if (int.TryParse(DamageTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.Damage = result;
				}

				if (int.TryParse(KnockBackXTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.KnockBackX = result;
				}

				if (int.TryParse(KnockBackYTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.KnockBackY = result;
				}

				if (int.TryParse(ProjectileSpeedXTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.ProjectileSpeedX = result;
				}

				if (int.TryParse(ProjectileSpeedYTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.ProjectileSpeedY = result;
				}

				if (int.TryParse(HitStunFramesTextBox.Text, out result))
				{
					previouslySelectedAnimationInformationBox.HitStunFrames = result;
				}

				previouslySelectedAnimationInformationBox.PopUp = PopUpCheckBox.IsChecked.HasValue ? PopUpCheckBox.IsChecked.Value : false;
				previouslySelectedAnimationInformationBox.ArcProjectile = ArcProjectileCheckBox.IsChecked.HasValue ? ArcProjectileCheckBox.IsChecked.Value : false;
			}
		}

		private void OnAnimationInformationBoxSelected(object sender, RoutedEventArgs e)
		{
			SavepreviouslySelectedAnimationInformationBoxValues();

			ListBoxItem item = (ListBoxItem)sender;

			ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
			string selectedAnimationName = selectedAnimationItem.Content.ToString();
			StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

			List<List<AnimationInformationBox>> listOfBoxes = null;

			if (item.Content.ToString().Contains("Hit"))
			{
				listOfBoxes = selectedAnimation.HitBoxPerFrame;

				DamageTextBox.IsEnabled = true;
				KnockBackXTextBox.IsEnabled = true;
				KnockBackYTextBox.IsEnabled = true;
				ProjectileSpeedXTextBox.IsEnabled = true;
				ProjectileSpeedYTextBox.IsEnabled = true;
				HitStunFramesTextBox.IsEnabled = true;
				PopUpCheckBox.IsEnabled = true;
				ArcProjectileCheckBox.IsEnabled = true;
			}
			else if (item.Content.ToString().Contains("Hurt"))
			{
				listOfBoxes = selectedAnimation.HurtBoxPerFrame;

				DamageTextBox.IsEnabled = false;
				KnockBackXTextBox.IsEnabled = false;
				KnockBackYTextBox.IsEnabled = false;
				ProjectileSpeedXTextBox.IsEnabled = false;
				ProjectileSpeedYTextBox.IsEnabled = false;
				HitStunFramesTextBox.IsEnabled = false;
				PopUpCheckBox.IsEnabled = false;
				ArcProjectileCheckBox.IsEnabled = false;
			}
			else if (item.Content.ToString().Contains("Projectile Spawn"))
			{
				listOfBoxes = selectedAnimation.ProjectileSpawnBoxPerFrame;

				DamageTextBox.IsEnabled = false;
				KnockBackXTextBox.IsEnabled = false;
				KnockBackYTextBox.IsEnabled = false;
				ProjectileSpeedXTextBox.IsEnabled = false;
				ProjectileSpeedYTextBox.IsEnabled = false;
				HitStunFramesTextBox.IsEnabled = false;
				PopUpCheckBox.IsEnabled = false;
				ArcProjectileCheckBox.IsEnabled = false;
			}

			for (int i = 0; i < listOfBoxes[CurrentFrame].Count; i++)
			{
				AnimationInformationBox box = listOfBoxes[CurrentFrame][i];

				if (box.Name == item.Content.ToString())
				{
					xTextBox.Text = box.Box.X.ToString();
					yTextBox.Text = box.Box.Y.ToString();
					wTextBox.Text = box.Box.Width.ToString();
					hTextBox.Text = box.Box.Height.ToString();
					DamageTextBox.Text = box.Damage.ToString();
					KnockBackXTextBox.Text = box.KnockBackX.ToString();
					KnockBackYTextBox.Text = box.KnockBackY.ToString();
					ProjectileSpeedXTextBox.Text = box.ProjectileSpeedX.ToString();
					ProjectileSpeedYTextBox.Text = box.ProjectileSpeedY.ToString();
					HitStunFramesTextBox.Text = box.HitStunFrames.ToString();
					PopUpCheckBox.IsChecked = box.PopUp;
					ArcProjectileCheckBox.IsChecked = box.ArcProjectile;

					break;
				}
			}
		}

		private void OnAnimationInformationBoxUnselected(object sender, RoutedEventArgs e)
		{
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			SavePreviouslySelectedAnimationsInformation(previouslySelectedAnimationsName);
			SavepreviouslySelectedAnimationInformationBoxValues();

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

		private string SelectSoundFile()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDialog.FilterIndex = 2;
			openFileDialog.RestoreDirectory = true;

			if (openFileDialog.ShowDialog() == true)
			{
				return openFileDialog.FileName;
			}

			return "";
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

					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.IdleAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.WalkingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.RunningAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.DyingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.DeadAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.AttackingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.BlockingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.HitStunAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.JumpingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.JumpApexAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.FallingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.LandingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.TalkingAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.ProjectileActiveAnimations);
					AddListBoxItemsForThisAnimationsAnimationInformationBoxes(unit.ProjectileHitAnimations);
				}

				FrameCanvas.Children.Clear();
				isDataDirty = false;
				//CreateUnitButton.IsEnabled = true;
			}
		}

		void AddListBoxItemsForThisAnimationsAnimationInformationBoxes(List<StateAnimation> anims)
		{
			for (int i = 0; i < anims.Count; i++)
			{
				for (int j = 0; j < anims[i].HitBoxPerFrame.Count; j++)
				{
					for (int k = 0; k < anims[i].HitBoxPerFrame[j].Count; k++)
					{
						AddListBoxItemsForThisAnimationsAnimationInformationBoxes(anims, i, j, k, "Hit");
					}
				}

				for (int j = 0; j < anims[i].HurtBoxPerFrame.Count; j++)
				{
					for (int k = 0; k < anims[i].HurtBoxPerFrame[j].Count; k++)
					{
						AddListBoxItemsForThisAnimationsAnimationInformationBoxes(anims, i, j, k, "Hurt");
					}
				}

				for (int j = 0; j < anims[i].ProjectileSpawnBoxPerFrame.Count; j++)
				{
					for (int k = 0; k < anims[i].ProjectileSpawnBoxPerFrame[j].Count; k++)
					{
						AddListBoxItemsForThisAnimationsAnimationInformationBoxes(anims, i, j, k, "Projectile Spawn");
					}
				}
			}
		}

		void AddListBoxItemsForThisAnimationsAnimationInformationBoxes(List<StateAnimation> anims, int i, int j, int k, string box_type)
		{
			ListBoxItem newListBoxItem = new ListBoxItem();
			if (box_type.Contains("Hit"))
			{
				newListBoxItem.Content = anims[i].HitBoxPerFrame[j][k].Name;
			}
			else if(box_type.Contains("Hurt"))
			{
				newListBoxItem.Content = anims[i].HurtBoxPerFrame[j][k].Name;
			}
			else if (box_type.Contains("Projectile Spawn"))
			{
				newListBoxItem.Content = anims[i].ProjectileSpawnBoxPerFrame[j][k].Name;
			}
			newListBoxItem.Selected += OnAnimationInformationBoxSelected;
			newListBoxItem.Unselected += OnAnimationInformationBoxUnselected;
			ListOfAnimationInformationBoxes.Items.Add(newListBoxItem);

			while (anims[i].AnimationInformationBoxListItems.Count <= j)
			{
				anims[i].AnimationInformationBoxListItems.Add(new List<ListBoxItem>());
			}

			if (anims[i].AnimationInformationBoxListItems.Count > j)
			{
				if (anims[i].AnimationInformationBoxListItems[j] == null)
				{
					anims[i].AnimationInformationBoxListItems[j] = new List<ListBoxItem>();
				}

				anims[i].AnimationInformationBoxListItems[j].Add(newListBoxItem);
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

		private int Poise
		{
			get
			{
				if (PoiseTextBox.Text == string.Empty)
				{
					return 0;
				}

				int poise = 1;
				if (int.TryParse(PoiseTextBox.Text, out poise))
				{
					return poise;
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
					if (!framesPerSecondTextBox.IsFocused)
					{
						framesPerSecondTextBox.Text = "120";
					}

					return 120;
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

		private void DeleteAnimationInformationBoxButton_Click(object sender, RoutedEventArgs e)
		{
			if (ListOfAnimationInformationBoxes.SelectedItem != null)
			{
				string sCaption = "Delete Animation Information Box";
				string sMessageBoxText = "This will delete the currently selected\nanimation information box and cannot be undone.\n\nDo you wish to continue?";

				MessageBoxButton btnMessageBox = MessageBoxButton.YesNoCancel;
				MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

				MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

				switch (rsltMessageBox)
				{
					case MessageBoxResult.Yes:
						ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
						string selectedAnimationName = selectedAnimationItem.Content.ToString();
						StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

						ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
						string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();

						if (selectedAnimationInformationBoxName.Contains("Hit"))
						{
							if (selectedAnimation.HitBoxPerFrame.Count > CurrentFrame)
							{
								for (int box = 0; box < selectedAnimation.HitBoxPerFrame[CurrentFrame].Count; box++)
								{
									if (selectedAnimationInformationBoxName == selectedAnimation.HitBoxPerFrame[CurrentFrame][box].Name)
									{
										FrameCanvas.Children.Remove(selectedAnimation.HitBoxPerFrame[CurrentFrame][box].DrawRectangle);
										selectedAnimation.HitBoxPerFrame[CurrentFrame][box].DrawRectangle = null;
										selectedAnimation.HitBoxPerFrame[CurrentFrame].RemoveAt(box);
										break;
									}
								}
							}
						}
						else if (selectedAnimationInformationBoxName.Contains("Hurt"))
						{
							if (selectedAnimation.HurtBoxPerFrame.Count > CurrentFrame)
							{
								for (int box = 0; box < selectedAnimation.HurtBoxPerFrame[CurrentFrame].Count; box++)
								{
									if (selectedAnimationInformationBoxName == selectedAnimation.HurtBoxPerFrame[CurrentFrame][box].Name)
									{
										FrameCanvas.Children.Remove(selectedAnimation.HurtBoxPerFrame[CurrentFrame][box].DrawRectangle);
										selectedAnimation.HurtBoxPerFrame[CurrentFrame][box].DrawRectangle = null;
										selectedAnimation.HurtBoxPerFrame[CurrentFrame].RemoveAt(box);
										break;
									}
								}
							}
						}
						else if (selectedAnimationInformationBoxName.Contains("Projectile Spawn"))
						{
							if (selectedAnimation.ProjectileSpawnBoxPerFrame.Count > CurrentFrame)
							{
								for (int box = 0; box < selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].Count; box++)
								{
									if (selectedAnimationInformationBoxName == selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame][box].Name)
									{
										FrameCanvas.Children.Remove(selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame][box].DrawRectangle);
										selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame][box].DrawRectangle = null;
										selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].RemoveAt(box);
										break;
									}
								}
							}
						}
						
						if (selectedAnimation.AnimationInformationBoxListItems.Count > CurrentFrame)
						{
							for (int box = 0; box < selectedAnimation.AnimationInformationBoxListItems[CurrentFrame].Count; box++)
							{
								if (selectedAnimationInformationBoxName == selectedAnimation.AnimationInformationBoxListItems[CurrentFrame][box].Content.ToString())
								{
									selectedAnimation.AnimationInformationBoxListItems[CurrentFrame].RemoveAt(box);
									break;
								}
							}
						}

						ListOfAnimationInformationBoxes.Items.Remove(selectedAnimationInformationBoxItem);
						
						//FrameCanvas.Children.Clear();
						//
						//if (selectedAnimation.HitBoxPerFrame.Count > CurrentFrame)
						//{
						//	for (int box = 0; box < selectedAnimation.HitBoxPerFrame[CurrentFrame].Count; box++)
						//	{
						//		if (selectedAnimationInformationBoxName == selectedAnimation.HitBoxPerFrame[CurrentFrame][box].Name)
						//		{
						//			FrameCanvas.Children.Add(selectedAnimation.HitBoxPerFrame[CurrentFrame][box].DrawRectangle);
						//		}
						//	}
						//}
						//
						//if (selectedAnimation.HurtBoxPerFrame.Count > CurrentFrame)
						//{
						//	for (int box = 0; box < selectedAnimation.HurtBoxPerFrame[CurrentFrame].Count; box++)
						//	{
						//		if (selectedAnimationInformationBoxName == selectedAnimation.HurtBoxPerFrame[CurrentFrame][box].Name)
						//		{
						//			FrameCanvas.Children.Add(selectedAnimation.HurtBoxPerFrame[CurrentFrame][box].DrawRectangle);
						//		}
						//	}
						//}
						//
						//if (selectedAnimation.ProjectileSpawnBoxPerFrame.Count > CurrentFrame)
						//{
						//	for (int box = 0; box < selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].Count; box++)
						//	{
						//		if (selectedAnimationInformationBoxName == selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame][box].Name)
						//		{
						//			FrameCanvas.Children.Add(selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame][box].DrawRectangle);
						//		}
						//	}
						//}

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
			if (ListOfAnimationInformationBoxes.SelectedItem != null)
			{
				ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
				string selectedAnimationName = selectedAnimationItem.Content.ToString();
				StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);

				if (CurrentFrame + 1 < selectedAnimation.NumberOfFrames)
				{
					ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
					string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();
					AnimationInformationBox origAnimationInformationBox = new AnimationInformationBox();

					if (selectedAnimationInformationBoxName.Contains("Hit"))
					{
						origAnimationInformationBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
					}
					else if (selectedAnimationInformationBoxName.Contains("Hurt"))
					{
						origAnimationInformationBox = selectedAnimation.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
					}
					else if (selectedAnimationInformationBoxName.Contains("Projectile Spawn"))
					{
						origAnimationInformationBox = selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
					}

					if (origAnimationInformationBox != null)
					{
						AnimationInformationBox newAnimationInformationBox = new AnimationInformationBox();
						newAnimationInformationBox.Box.X = origAnimationInformationBox.Box.X;
						newAnimationInformationBox.Box.Y = origAnimationInformationBox.Box.Y;
						newAnimationInformationBox.Box.Width = origAnimationInformationBox.Box.Width;
						newAnimationInformationBox.Box.Height = origAnimationInformationBox.Box.Height;
						newAnimationInformationBox.DrawRect.X = origAnimationInformationBox.DrawRect.X;
						newAnimationInformationBox.DrawRect.Y = origAnimationInformationBox.DrawRect.Y;
						newAnimationInformationBox.DrawRect.Width = origAnimationInformationBox.DrawRect.Width;
						newAnimationInformationBox.DrawRect.Height = origAnimationInformationBox.DrawRect.Height;
						newAnimationInformationBox.DrawRectangle = origAnimationInformationBox.DrawRectangle;
						newAnimationInformationBox.Damage = origAnimationInformationBox.Damage;
						newAnimationInformationBox.KnockBackX = origAnimationInformationBox.KnockBackX;
						newAnimationInformationBox.KnockBackY = origAnimationInformationBox.KnockBackY;
						newAnimationInformationBox.ProjectileSpeedX = origAnimationInformationBox.ProjectileSpeedX;
						newAnimationInformationBox.ProjectileSpeedY = origAnimationInformationBox.ProjectileSpeedY;
						newAnimationInformationBox.HitStunFrames = origAnimationInformationBox.HitStunFrames;
						newAnimationInformationBox.PopUp = origAnimationInformationBox.PopUp;
						newAnimationInformationBox.ArcProjectile = origAnimationInformationBox.ArcProjectile;
						newAnimationInformationBox.Frame = CurrentFrame + 1;

						AddNewAnimationInformationBox(selectedAnimation, newAnimationInformationBox, CurrentFrame + 1, selectedAnimationInformationBoxName, false);
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

		private void JumpingSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				SelectedUnit.JumpingSoundFile = SelectSoundFile();
				JumpingSoundFileTextBox.Text = SelectedUnit.JumpingSoundFile;
			}
		}

		private void LandingSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				SelectedUnit.LandingSoundFile = SelectSoundFile();
				LandingSoundFileTextBox.Text = SelectedUnit.LandingSoundFile;
			}
		}

		private void LeftFootSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				StateAnimation anim = GetCurrentStateAnimation();

				if (anim != null)
				{
					while (anim.LeftFootSoundFilePerFrame.Count <= CurrentFrame)
					{
						anim.LeftFootSoundFilePerFrame.Add("");
					}

					if (anim.LeftFootSoundFilePerFrame.Count > CurrentFrame)
					{
						anim.LeftFootSoundFilePerFrame[CurrentFrame] = SelectSoundFile();
						LeftFootSoundFileTextBox.Text = anim.LeftFootSoundFilePerFrame[CurrentFrame];
					}
				}
			}
		}

		private void RightFootSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				StateAnimation anim = GetCurrentStateAnimation();

				if (anim != null)
				{
					while (anim.RightFootSoundFilePerFrame.Count <= CurrentFrame)
					{
						anim.RightFootSoundFilePerFrame.Add("");
					}

					if (anim.RightFootSoundFilePerFrame.Count > CurrentFrame)
					{
						anim.RightFootSoundFilePerFrame[CurrentFrame] = SelectSoundFile();
						RightFootSoundFileTextBox.Text = anim.RightFootSoundFilePerFrame[CurrentFrame];
					}
				}
			}
		}

		private void AddGettingHitSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				string new_getting_hit_sound_file = SelectSoundFile();

				ListBoxItem new_item = new ListBoxItem();
				new_item.Content = new_getting_hit_sound_file;

				ListOfGettingHitSoundFiles.Items.Add(new_item);

				SelectedUnit.GettingHitSoundFiles.Add(new_getting_hit_sound_file);
			}
		}

		private void DeleteGettingHitSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				ListBoxItem selected_item = (ListBoxItem)ListOfGettingHitSoundFiles.SelectedItem;
				if (selected_item != null)
				{
					for (int i = 0; i < SelectedUnit.GettingHitSoundFiles.Count; i++)
					{
						if (SelectedUnit.GettingHitSoundFiles[i] == selected_item.Content.ToString())
						{
							SelectedUnit.GettingHitSoundFiles.RemoveAt(i);
							break;
						}
					}

					ListOfGettingHitSoundFiles.Items.Remove(selected_item);
				}
			}
		}

		private void SwingingSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				StateAnimation anim = GetCurrentStateAnimation();

				if (anim != null)
				{
					while (anim.AttackSoundFilePerFrame.Count <= CurrentFrame)
					{
						anim.AttackSoundFilePerFrame.Add("");
					}

					if (anim.AttackSoundFilePerFrame.Count > CurrentFrame)
					{
						anim.AttackSoundFilePerFrame[CurrentFrame] = SelectSoundFile();
						SwingingSoundFileTextBox.Text = anim.AttackSoundFilePerFrame[CurrentFrame];
					}
				}
			}
		}

		private void ThrowProjectileSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				StateAnimation anim = GetCurrentStateAnimation();

				if (anim != null)
				{
					while (anim.ThrowProjectileSoundFilePerFrame.Count <= CurrentFrame)
					{
						anim.ThrowProjectileSoundFilePerFrame.Add("");
					}

					if (anim.ThrowProjectileSoundFilePerFrame.Count > CurrentFrame)
					{
						anim.ThrowProjectileSoundFilePerFrame[CurrentFrame] = SelectSoundFile();
						ThrowProjectileSoundFileTextBox.Text = anim.ThrowProjectileSoundFilePerFrame[CurrentFrame];
					}
				}
			}
		}

		private void ProjectileHitSoundFileButton_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				StateAnimation anim = GetCurrentStateAnimation();

				if (anim != null)
				{
					while (anim.ProjectileHitSoundFilePerFrame.Count <= CurrentFrame)
					{
						anim.ProjectileHitSoundFilePerFrame.Add("");
					}

					if (anim.ProjectileHitSoundFilePerFrame.Count > CurrentFrame)
					{
						anim.ProjectileHitSoundFilePerFrame[CurrentFrame] = SelectSoundFile();
						ProjectileHitSoundFileTextBox.Text = anim.ProjectileHitSoundFilePerFrame[CurrentFrame];
					}
				}
			}
		}

		private void CopyBoxToEveryFrameButton_Click(object sender, RoutedEventArgs e)
		{
			if (ListOfAnimationInformationBoxes.SelectedItem != null)
			{
				ListBoxItem selectedAnimationItem = (ListBoxItem)ListOfStateAnimations.SelectedItem;
				string selectedAnimationName = selectedAnimationItem.Content.ToString();
				StateAnimation selectedAnimation = GetCurrentStateAnimation(selectedAnimationName);
				
				ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
				string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();
				AnimationInformationBox origAnimationInformationBox = new AnimationInformationBox();

				if (selectedAnimationInformationBoxName.Contains("Hit"))
				{
					origAnimationInformationBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
				}
				else if (selectedAnimationInformationBoxName.Contains("Hurt"))
				{
					origAnimationInformationBox = selectedAnimation.HurtBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
				}
				else if (selectedAnimationInformationBoxName.Contains("Projectile Spawn"))
				{
					origAnimationInformationBox = selectedAnimation.ProjectileSpawnBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
				}

				if (origAnimationInformationBox != null)
				{
					for (int i = 0; i < NumberOfFrames; i++)
					{
						if (i != CurrentFrame)
						{
							AnimationInformationBox newAnimationInformationBox = new AnimationInformationBox();
							newAnimationInformationBox.Box.X = origAnimationInformationBox.Box.X;
							newAnimationInformationBox.Box.Y = origAnimationInformationBox.Box.Y;
							newAnimationInformationBox.Box.Width = origAnimationInformationBox.Box.Width;
							newAnimationInformationBox.Box.Height = origAnimationInformationBox.Box.Height;
							newAnimationInformationBox.DrawRect.X = origAnimationInformationBox.DrawRect.X;
							newAnimationInformationBox.DrawRect.Y = origAnimationInformationBox.DrawRect.Y;
							newAnimationInformationBox.DrawRect.Width = origAnimationInformationBox.DrawRect.Width;
							newAnimationInformationBox.DrawRect.Height = origAnimationInformationBox.DrawRect.Height;
							newAnimationInformationBox.DrawRectangle = origAnimationInformationBox.DrawRectangle;
							newAnimationInformationBox.Damage = origAnimationInformationBox.Damage;
							newAnimationInformationBox.KnockBackX = origAnimationInformationBox.KnockBackX;
							newAnimationInformationBox.KnockBackY = origAnimationInformationBox.KnockBackY;
							newAnimationInformationBox.ProjectileSpeedX = origAnimationInformationBox.ProjectileSpeedX;
							newAnimationInformationBox.ProjectileSpeedY = origAnimationInformationBox.ProjectileSpeedY; 
							newAnimationInformationBox.HitStunFrames = origAnimationInformationBox.HitStunFrames;
							newAnimationInformationBox.PopUp = origAnimationInformationBox.PopUp;
							newAnimationInformationBox.ArcProjectile = origAnimationInformationBox.ArcProjectile;
							newAnimationInformationBox.Frame = i;

							AddNewAnimationInformationBox(selectedAnimation, newAnimationInformationBox, i, selectedAnimationInformationBoxName, false);
						}
					}
				}
			}
		}

		private void HitBoxCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (HurtBoxCheckBox != null && HurtBoxCheckBox.IsChecked != null)
				HurtBoxCheckBox.IsChecked = false;
			if (ProjectileSpawnCheckBox != null && ProjectileSpawnCheckBox.IsChecked != null)
				ProjectileSpawnCheckBox.IsChecked = false;
		}

		private void ProjectileSpawnCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (HurtBoxCheckBox != null && HurtBoxCheckBox.IsChecked != null)
				HurtBoxCheckBox.IsChecked = false;
			if (HitBoxCheckBox != null && HitBoxCheckBox.IsChecked != null)
				HitBoxCheckBox.IsChecked = false;
		}

		private void HurtBoxCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (HitBoxCheckBox != null && HitBoxCheckBox.IsChecked != null)
				HitBoxCheckBox.IsChecked = false;
			if (ProjectileSpawnCheckBox != null && ProjectileSpawnCheckBox.IsChecked != null)
				ProjectileSpawnCheckBox.IsChecked = false;
		}

		private void PopUpCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			StateAnimation selectedAnimation = GetCurrentStateAnimation();
			if (selectedAnimation != null)
			{
				if (ListOfAnimationInformationBoxes.SelectedItem != null)
				{
					ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
					string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();
					AnimationInformationBox selectedAnimationInformationBox = null;

					if (selectedAnimationInformationBoxName.Contains("Hit"))
					{
						selectedAnimationInformationBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
					}

					if (selectedAnimationInformationBox != null)
					{
						selectedAnimationInformationBox.PopUp = PopUpCheckBox.IsChecked.HasValue ? PopUpCheckBox.IsChecked.Value : false;
					}
				}
			}
		}

		private void FlyingCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (SelectedUnit != null)
			{
				SelectedUnit.Flying = FlyingCheckBox.IsChecked.HasValue ? FlyingCheckBox.IsChecked.Value : false;
			}
		}

		private void ArcProjectileCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			StateAnimation selectedAnimation = GetCurrentStateAnimation();
			if (selectedAnimation != null)
			{
				if (ListOfAnimationInformationBoxes.SelectedItem != null)
				{
					ListBoxItem selectedAnimationInformationBoxItem = (ListBoxItem)ListOfAnimationInformationBoxes.SelectedItem;
					string selectedAnimationInformationBoxName = selectedAnimationInformationBoxItem.Content.ToString();
					AnimationInformationBox selectedAnimationInformationBox = null;

					if (selectedAnimationInformationBoxName.Contains("Hit"))
					{
						selectedAnimationInformationBox = selectedAnimation.HitBoxPerFrame[CurrentFrame].Where(x => x.Name == selectedAnimationInformationBoxName).First();
					}

					if (selectedAnimationInformationBox != null)
					{
						selectedAnimationInformationBox.ArcProjectile = ArcProjectileCheckBox.IsChecked.HasValue ? ArcProjectileCheckBox.IsChecked.Value : false;
					}
					else
					{
						ArcProjectileCheckBox.IsChecked = false;
					}
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
		public float InteractionRadius = 0.0f;
		public bool Flying = false;
		
		public string JumpingSoundFile = "";
		public string LandingSoundFile = "";
		public List<string> GettingHitSoundFiles = new List<string>();

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
		public List<StateAnimation> TalkingAnimations = new List<StateAnimation>();
		public List<StateAnimation> ProjectileActiveAnimations = new List<StateAnimation>();
		public List<StateAnimation> ProjectileHitAnimations = new List<StateAnimation>();

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
		public int Poise = 0;
		public List<string> AttackSoundFilePerFrame = new List<string>();
		public List<string> RightFootSoundFilePerFrame = new List<string>();
		public List<string> LeftFootSoundFilePerFrame = new List<string>();
		public List<string> ThrowProjectileSoundFilePerFrame = new List<string>();
		public List<string> ProjectileHitSoundFilePerFrame = new List<string>();
		public List<List<AnimationInformationBox>> HurtBoxPerFrame = new List<List<AnimationInformationBox>>();
		public List<List<AnimationInformationBox>> HitBoxPerFrame = new List<List<AnimationInformationBox>>();
		public List<List<AnimationInformationBox>> ProjectileSpawnBoxPerFrame = new List<List<AnimationInformationBox>>();
		[JsonIgnore]
		public List<List<ListBoxItem>> AnimationInformationBoxListItems = new List<List<ListBoxItem>>();

		public StateAnimation()
		{
		}
	}

	public class AnimationInformationBox
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
		public float ProjectileSpeedX = 0.0f;
		public float ProjectileSpeedY = 0.0f;
		public int HitStunFrames = 0;
		public bool PopUp = false;
		public bool ArcProjectile = false;

		public AnimationInformationBox()
		{
		}
	}
}
