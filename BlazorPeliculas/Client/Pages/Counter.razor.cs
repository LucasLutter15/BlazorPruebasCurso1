using BlazorPeliculas.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorPeliculas.Client.Pages
{
    public partial class Counter
    {

        private int currentCount = 0;
        [Inject] private IJSRuntime js { get; set; }
        public async void IncrementCount()
        {
            var arreglo = new double[] { 1, 2, 3, 4, 5 };
            var max = arreglo.Max();
            var min = arreglo.Min();
            await js.InvokeVoidAsync("alert", $"El max es {max} y el min es {min}");
            currentCount++;
        }
    }
}
