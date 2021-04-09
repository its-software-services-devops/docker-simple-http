using Xunit;

namespace simple_http
{
    public class SomeCalculationsTest
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        [InlineData(-1, -1)]        
        public void DoAddTest(int a, int b)
        {
            int expected = a + b;
            int calculatedResult = SomeCalculations.DoAdd(a, b);
            Assert.Equal(expected, calculatedResult);
        } 

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, -1)]
        [InlineData(-1, -1)]        
        public void DoSubtractTest(int a, int b)
        {
            int expected = a - b;
            int calculatedResult = SomeCalculations.DoSubtract(a, b);
            Assert.Equal(expected, calculatedResult);
        } 

        [Theory]
        [InlineData(10, 1)]
        [InlineData(5, 2)]
        [InlineData(3, 7)]
        [InlineData(7, 7)]        
        public void DoModTest(int a, int b)
        {
            int expected = a % b;
            int calculatedResult = SomeCalculations.DoMod(a, b);
            Assert.Equal(expected, calculatedResult);
        }

        [Theory]
        [InlineData(10, 0)]
        public void DoModByZeroTest(int a, int b)
        {
            int expected = 0;
            int calculatedResult = SomeCalculations.DoMod(a, b);
            Assert.Equal(expected, calculatedResult);
        }                             
    }     
}
