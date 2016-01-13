CONFIG?=Debug

all:
	cd external; make 
	xbuild NRefactory.sln /p:Configuration=${CONFIG} ${ARGS}

clean:
	cd external; make clean
	xbuild NRefactory.sln /p:Configuration=${CONFIG} ${ARGS}
