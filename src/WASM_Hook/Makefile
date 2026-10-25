# Set this only after the MSFS 2024 SDK confirms SimConnect Client Data support in gauge WASM.
ifndef MSFS_SDK
$(error Set MSFS_SDK to the installed MSFS SDK path)
endif
ifndef MSFS_WASM_SIMCONNECT_SUPPORTED
$(error Refusing to build: set MSFS_WASM_SIMCONNECT_SUPPORTED=1 only after validating supported WASM Client Data APIs)
endif

# Integrate these sources into the SDK's official gauge-package template. The exact
# compiler/linker settings vary by SDK release and are intentionally not guessed here.
all:
	@echo Integrate Wasm_Hook.cpp with the installed MSFS SDK gauge template.

