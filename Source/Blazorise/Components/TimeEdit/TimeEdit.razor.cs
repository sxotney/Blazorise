﻿#region Using directives
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Blazorise.Extensions;
using Blazorise.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
#endregion

namespace Blazorise
{
    /// <summary>
    /// An editor that displays a time value and allows a user to edit the value.
    /// </summary>
    /// <typeparam name="TValue">Data-type to be binded by the <see cref="Value"/> property.</typeparam>
    public partial class TimeEdit<TValue> : BaseTextInput<TValue>
    {
        #region Methods

        /// <inheritdoc/>
        public override async Task SetParametersAsync( ParameterView parameters )
        {
            if ( parameters.TryGetValue<TValue>( nameof( Time ), out var time )
                && !Time.IsEqual( time ) )
            {
                var timeString = FormatValueAsString( time );

                await CurrentValueHandler( timeString );

                if ( Rendered )
                {
                    // In case a provider needs to update it's own implementation but only after the component was
                    // rendered, meaning it was already initialized.
                    ExecuteAfterRender( async () => await JSRunner.UpdateTimePicker( ElementRef, ElementId, timeString ) );
                }
            }

            await base.SetParametersAsync( parameters );

            if ( ParentValidation != null )
            {
                if ( parameters.TryGetValue<Expression<Func<TValue>>>( nameof( TimeExpression ), out var expression ) )
                    ParentValidation.InitializeInputExpression( expression );

                if ( parameters.TryGetValue<string>( nameof( Pattern ), out var pattern ) )
                {
                    // make sure we get the newest value
                    var value = parameters.TryGetValue<TValue>( nameof( Time ), out var inTime )
                        ? inTime
                        : InternalValue;

                    ParentValidation.InitializeInputPattern( pattern, value );
                }

                InitializeValidation();
            }
        }

        protected override async Task OnFirstAfterRenderAsync()
        {
            await JSRunner.InitializeTimePicker( ElementRef, ElementId, new
            {
                DefaultValue = FormatValueAsString( Time )
            } );

            await base.OnFirstAfterRenderAsync();
        }

        /// <inheritdoc/>
        protected override async ValueTask DisposeAsync( bool disposing )
        {
            if ( disposing )
            {
                if ( Rendered )
                {
                    var task = JSRunner.DestroyTimePicker( ElementRef, ElementId );

                    try
                    {
                        await task;
                    }
                    catch
                    {
                        if ( !task.IsCanceled )
                        {
                            throw;
                        }
                    }
                }
            }

            await base.DisposeAsync( disposing );
        }

        /// <inheritdoc/>
        protected override void BuildClasses( ClassBuilder builder )
        {
            builder.Append( ClassProvider.TimeEdit( Plaintext ) );
            builder.Append( ClassProvider.TimeEditSize( Size ), Size != Size.None );
            builder.Append( ClassProvider.TimeEditColor( Color ), Color != Color.None );
            builder.Append( ClassProvider.TimeEditValidation( ParentValidation?.Status ?? ValidationStatus.None ), ParentValidation?.Status != ValidationStatus.None );

            base.BuildClasses( builder );
        }

        /// <inheritdoc/>
        protected override Task OnChangeHandler( ChangeEventArgs e )
        {
            return CurrentValueHandler( e?.Value?.ToString() );
        }

        protected async Task OnClickHandler( MouseEventArgs e )
        {
            if ( Disabled || ReadOnly )
                return;

            await JSRunner.ActivateTimePicker( ElementRef, ElementId, Parsers.InternalTimeFormat );
        }

        /// <inheritdoc/>
        protected override Task OnInternalValueChanged( TValue value )
        {
            return TimeChanged.InvokeAsync( value );
        }

        /// <inheritdoc/>
        protected override string FormatValueAsString( TValue value )
        {
            switch ( value )
            {
                case null:
                    return null;
                case TimeSpan timeSpan:
                    return timeSpan.ToString( Parsers.InternalTimeFormat );
                case DateTime datetime:
                    return datetime.ToString( Parsers.InternalTimeFormat );
                default:
                    throw new InvalidOperationException( $"Unsupported type {value.GetType()}" );
            }
        }

        protected override Task<ParseValue<TValue>> ParseValueFromStringAsync( string value )
        {
            if ( Parsers.TryParseTime<TValue>( value, out var result ) )
            {
                return Task.FromResult( new ParseValue<TValue>( true, result, null ) );
            }
            else
            {
                return Task.FromResult( new ParseValue<TValue>( false, default, null ) );
            }
        }

        /// <inheritdoc/>
        protected override Task OnKeyPressHandler( KeyboardEventArgs eventArgs )
        {
            // just call eventcallback without using debouncer in BaseTextInput
            return KeyPress.InvokeAsync( eventArgs );
        }

        /// <inheritdoc/>
        protected override Task OnBlurHandler( FocusEventArgs eventArgs )
        {
            // just call eventcallback without using debouncer in BaseTextInput
            return Blur.InvokeAsync( eventArgs );
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        protected override bool ShouldAutoGenerateId => true;

        /// <inheritdoc/>
        protected override TValue InternalValue { get => Time; set => Time = value; }

        /// <summary>
        /// Gets or sets the input time value.
        /// </summary>
        [Parameter] public TValue Time { get; set; }

        /// <summary>
        /// Occurs when the time has changed.
        /// </summary>
        [Parameter] public EventCallback<TValue> TimeChanged { get; set; }

        /// <summary>
        /// Gets or sets an expression that identifies the time field.
        /// </summary>
        [Parameter] public Expression<Func<TValue>> TimeExpression { get; set; }

        #endregion
    }
}
