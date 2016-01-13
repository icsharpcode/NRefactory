CONFIG?=Debug

all:
	cd cecil; xbuild Mono.Cecil.sln /p:Configuration=net_4_5_${CONFIG} ${ARGS}
	cd NRefactory; xbuild NRefactory.sln /p:Configuration=${CONFIG} ${ARGS}

clean:
	cd cecil; xbuild Mono.Cecil.sln /p:Configuration=net_4_5_${CONFIG} /target:Clean
	cd NRefactory; xbuild NRefactory.sln /p:Configuration=${CONFIG} /target:Clean 
