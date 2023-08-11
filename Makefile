all: main linux_build linux_publish copy_bins_to_toplevel linux_test

main:
	@echo "Building main... ---"
	dotnet build likeNotepad

linux_build:
	@echo "Building linux_build... ---"
	dotnet build likeNotepad.Gtk -p:PublishSingleFile=true --self-contained true

linux_publish:
	@echo "Building linux_publish... ---"
	dotnet publish likeNotepad.Gtk -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true

copy_bins_to_toplevel:
	mv likeNotepad.Gtk/bin/Release/net6.0/linux-x64/publish/likeNotepad.Gtk ./likeNotepad_linux64

linux_test:
	./likeNotepad_linux64