namespace SourceGenUtils.Sample.Sample;

// This code will not compile until you build the project with the Source Generators

[SampleGenerator]
public partial class ExampleClass { }

[SampleGenerator]
public partial class ExampleGenericClass<TArg1, TArg2> { }

[SampleGenerator]
public partial struct ExampleStruct { }

[SampleGenerator]
public partial struct ExampleGenericStruct<TArg1, TArg2> { }

[SampleGenerator]
public readonly partial struct ExampleReadonlyStruct { }

[SampleGenerator]
public readonly partial struct ExampleReadonlyGenericStruct<TArg1, TArg2> { }

[SampleGenerator]
public partial record ExampleRecord { }

[SampleGenerator]
public partial record ExampleGenericRecord<TArg1, TArg2> { }

[SampleGenerator]
public partial record class ExampleRecordClass { }

[SampleGenerator]
public partial record class ExampleGenericRecordClass<TArg1, TArg2> { }

[SampleGenerator]
public partial record struct ExampleRecordStruct { }

[SampleGenerator]
public partial record struct ExampleGenericRecordStruct<TArg1, TArg2> { }

[SampleGenerator]
public readonly partial record struct ExampleReadonlyRecordStruct { }

[SampleGenerator]
public readonly partial record struct ExampleReadonlyGenericRecordStruct<TArg1, TArg2> { }

[SampleGenerator]
public static partial class ExampleStaticClass { }