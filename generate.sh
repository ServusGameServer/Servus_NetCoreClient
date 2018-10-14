protoc -I=../Servus_Protobuf/lib/Base/protocolbuffers/ --csharp_out=. ServusProtobufMain.proto
cp ServusProtobufMain.cs ServusProtobuf/ServusProtobufMain.cs