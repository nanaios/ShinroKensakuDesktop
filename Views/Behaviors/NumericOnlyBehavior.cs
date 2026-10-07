using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ShinroKensakuDesktop.Views.Behaviors
{
	public class NumericOnlyBehavior : Behavior<TextBox>
	{
		protected override void OnAttached ( )
		{
			base.OnAttached ( );

			AssociatedObject.PreviewTextInput += OnPreviewTextInput;
			DataObject.AddPastingHandler ( AssociatedObject, OnPasting );
		}

		protected override void OnDetaching ( )
		{
			AssociatedObject.PreviewTextInput -= OnPreviewTextInput;
			DataObject.RemovePastingHandler ( AssociatedObject, OnPasting );

			base.OnDetaching ( );
		}

		private void OnPreviewTextInput ( object sender, TextCompositionEventArgs e ) =>
			e.Handled = e.Text.Any ( c => c is < '0' or > '9' );

		private void OnPasting ( object sender, DataObjectPastingEventArgs e )
		{
			if ( !e.DataObject.GetDataPresent ( DataFormats.Text ) )
			{
				e.CancelCommand ( );
				return;
			}

			string? text = e.DataObject.GetData ( DataFormats.Text ) as string;

			if ( text == null || text.Any ( c => c is < '0' or > '9' ) ||
			     ( AssociatedObject.MaxLength > 0 &&
			       AssociatedObject.Text.Length - AssociatedObject.SelectionLength + text.Length >
			       AssociatedObject.MaxLength ) )
			{
				e.CancelCommand ( );
			}
		}
	}
}