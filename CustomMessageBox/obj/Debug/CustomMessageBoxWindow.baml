   M S B A M L   `   `   `  ����  WPFCustomMessageBox0   *WPFCustomMessageBox.CustomMessageBoxWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � WindowStartupLocation$  CenterScreen=�5   	    � WindowStyle$ SingleBorderWindow=�5   	    � Icon$ blankicon.ico��5   	    � ShowInTaskbar .   � Topmost .  � 
ResizeMode$ NoResize=�5	   	    � SizeToContent$ WidthAndHeight=�6   &   System.Windows.Media.TextOptions   TextFormattingMode$ Display=�5
   	      TextRenderingMode$ 	ClearType=�62   	 � UseLayoutRounding	 . 
 � Title$
  ��5   	   $	��155��6   $	��470��6"   $	��154��61   .��5      �  � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   � $��0��6   
�������$��69��6/   .+�5      ]� .+�5      �� -   5        $��Image_MessageBox��5      $��32��60   $��32��6;   $
��Left=�6G   $
��Left=�6b   $��30,0,0,0q�6x   $y�	Collapsed=�6�   �� 5      -   5        $��TextBlock_Message��5      $
��Wrap=�65   $	��500��6I   $
��Auto��6X   $��Center=�5      $��12,20,41,15q�63   5      5      � 5   
   $��1��6   #��� ��6   $��49��6c   .+�5      ]� $	��5,0q�6   .+�5      �� -   5        $��Button_Cancel��5      $��88��6.   $	��160��6<   $��26��6K   $	��5,0q�6W   $��Right=�6d   $y�	Collapsed=�6�    �� IsCancel . $��Right=�5      .��5      �� -   5        $��Label_Cancel��5      $��0q�60   $
��10,0q�6<   .��6J   	_Cancel6S   5       �� 5$      -   5        $��	Button_No��5$      $��88��6*   $	��160��68   $��26��6G   $	��5,0q�6S   $��Right=�6`   $y�	Collapsed=�6|   $��Right=�5%      .��5&      �� -   5        $��Label_No��5&      $��0q�6,   $
��10,0q�68   .��6F   _No6K   5'      �� 5+      -   5        $��
Button_Yes��5+      $��88��6+   $	��160��69   $��26��6H   $��35,0,5,0q�6T   $��Right=�6f   $y�	Collapsed=�6�   $��Right=�5,      .��5-      �� -   5        $��	Label_Yes��5-      $��0q�6-   $
��10,0q�69   .��6G   _Yes6M   5.      �� 52      -	   5        $��	Button_OK��52      $��88��6*   $	��160��68   $��35,0,5,0q�6G   $��Right=�6Y   $��26��6u   .��54      �� -
   5        $��Label_Ok��54      $��0q�6,   $
��10,0q�68   .��6F   _OK6K   55      58      59      5:      5;      