class MyClass{


    /// <summary>
    /// Получает строку приветствия через вызов DataProcessor.Do и выводит её в консоль.
    /// </summary>
    public void Do(){
        var str = DataProcessor.Do();

        Console.WriteLine(str);
    }
}