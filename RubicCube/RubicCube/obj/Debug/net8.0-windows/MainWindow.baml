   M S B A M L   `   `   `  ����D  @RubicCube, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null%clr-namespace:RubicCube	RubicCube  5   	       RubicCube.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   )localclr-namespace:RubicCube     5   	   X THelixToolkit.Wpf, Version=2.25.0.0, Culture=neutral, PublicKeyToken=52aa3500039caf0d$hhttp://helix-toolkit.org/wpf  5   	     � Title$  
MainWindow��5	   	   $	��450��6   $	��800��6)   .��5
      �  � ColumnDefinitions 5   
   �� 5      $��* �6   6+   �� 5      $��2* �6   6,   .+�5   
   � .+�5      �� .+�5      �� Change Color6*   	� 5      -   5         	�Name$ RGBtoCMYKCheckBox��5       d� 	IsChecked$
 TrueY�6V   .��6g   RGB to CMYK6t   	� 5      -   5        $ CMYKtoRGBCheckBox��5      .��6V   CMYK to RGB6c   �� 5      -   5        $��RGBInputsPanel��5      .+�5      �� $	��RGB��6   6)   �� 5      -   5        $��	redSlider��5       � Value$ 0Q�6*    � Minimum$ 0Q�64    � Maximum$	 255Q�6@    �� TickFrequency$ 1Q�6N    �� IsSnapToTickEnabled$
 True��6`   6{   �� 5      -   5        $��greenSlider��5      $ 0Q�6,   $ 0Q�66   $	 255Q�6B   $ 1Q�6P   $
 True��6b   6}   �� 5      -   5        $��
blueSlider��5      $ 0Q�6+   $ 0Q�65   $	 255Q�6A   $ 1Q�6O   $
 True��6a   6|   �� 5      	 �� Orientation$	 
Horizontal=�6   .+�5      �� $��R:��6    �� 5      -   5        $��
redTextBox��5      $��0��60   69   �� 5      $��G:��6    �� 5      -   5        $��greenTextBox��5      $��0��62   6;   �� 5      $��B:��6    �� 5      -	   5        $��blueTextBox��5      $��0��61   6:   5       5!      �� 5"      -
   5        $��CMYKInputsPanel��5"      .+�5#      �� $
��CMYK��6   6*   �� 5$      -   5        $��
cyanSlider��5$      $ 0Q�6+   $ 0Q�65   $	 100Q�6A   $ 1Q�6O   $
 True��6a   6|   �� 5%      -   5        $��magentaSlider��5%      $ 0Q�6.   $ 0Q�68   $	 100Q�6D   $ 1Q�6R   $
 True��6d   6   �� 5&      -   5        $��yellowSlider��5&      $ 0Q�6-   $ 0Q�67   $	 100Q�6C   $ 1Q�6Q   $
 True��6c   6~   �� 5'      -   5        $��blackSlider��5'      $ 0Q�6,   $ 0Q�66   $	 100Q�6B   $ 1Q�6P   $
 True��6b   6}   �� 5(      $	 
Horizontal=�6   .+�5)      �� $��C:��6    �� 5*      -   5        $��cyanTextBox��5*      $��0��61   6:   �� 5+      $��M:��6    �� 5,      -   5        $��magentaTextBox��5,      $��0��64   6=   �� 5-      $��Y:��6    �� 5.      -   5        $��yellowTextBox��5.      $��0��63   6<   �� 5/      $��K:��6    �� 50      -   5        $��blackTextBox��50      $��0��62   6;   51      52      �� 53      $��Color��6   6+   �� 54      -   5        $��convertedColorDisplay��54      $��50��69   $��50��6D   �� 55      -   5        
 ��Name$
 colorCodeTextBlock��55      $��Center=�68   $��Center=�6U   6r   56      57      � 58   
   $��1��6   .+�59      &   HelixToolkit.Wpf.HelixViewport3D  -   5          Name$ 
viewport3D��59       $��1��64      IsZoomEnabled$
 True��5:       5;      5<      5=      5>      