namespace simple_http
{
    public static class SomeCalculations
    {
        public static int DoAdd(int a, int b)
        {
            return a+b;
        }

        public static int DoSubtract(int a, int b)
        {
            return a-b;
        }

        public static int DoMod(int a, int b)
        {
            if (b == 0)
            {
                return 0;
            }

            return a%b;
        }
    }
}