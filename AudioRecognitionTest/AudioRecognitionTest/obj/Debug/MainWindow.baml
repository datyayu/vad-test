   M S B A M L   `   `   `  ����O  KAudioRecognitionTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null;"clr-namespace:AudioRecognitionTestAudioRecognitionTest  5   	   %    AudioRecognitionTest.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   4local"clr-namespace:AudioRecognitionTest     5   	     � Title$  AudioRecognitionTest��5   	   $	��350��6&   $	��525��63   .��5	      �  � ColumnDefinitions 5
   
   �� 5      $��* �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $��* �6   �� 5      $��* �6   .+�5   
   �� -   5        $��RecordButton��5      $��Record��5      $��14(�5       � Cursor$
 Hand��5      5      �� 5   
   -   5        $��
StopButton��5      $��1��5      $
��Stop��5      $��14(�5      $
 Hand��5      
�������5!      �� 5$   
   $��0��6   $��1��5%      $��Center=�5&      $��Center=�5'      $��Voice Detected:��5(      �� 5+   
   $��1��6   $��1��5,      $��Center=�5-      $��20(�5.      $��Center=�5/      ��50      �� *audioRecognition.VadDetected+ �� UpdateSourceTrigger$ PropertyChanged=��� 54   
   $��0��6   $��2��55      $��Center=�56      $��Center=�57      $��
Threshold:��58      �� 5;   
   $��1��6   $��2��5<      $��Center=�5=      $��20(�5>      $��Center=�5?      ��5@      �� *$"audioRecognition.ThresholdDetected+$ PropertyChanged=�5C      5D      