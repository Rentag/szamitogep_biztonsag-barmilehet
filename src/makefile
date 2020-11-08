CPPFILES = $(wildcard *.cpp)
OBJFILES = $(CPPFILES:.cpp=.o)

CFLAGS = -Wall -std=c++11 -Wdeprecated -pedantic
LDLIBS = -lstdc++ -lm

main: $(OBJFILES)
	g++ $(CFLAGS) -o "CaffToCiff" $(OBJFILES)

.PHONY: clean
clean:
	rm -rf $(OBJFILES) CaffToCiff