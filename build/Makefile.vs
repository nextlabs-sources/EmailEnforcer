# =============================================================================
# Top-level makefile include by makefiles that wrap around VisualStudio projects
# =============================================================================

SIGNTOOL_OFFICIAL=c:/release/bin/nlsigntool.exe
# MSVSIDE="C:/Program Files (x86)/Microsoft Visual Studio 12.0/common7/IDE/devenv.exe"
MSVSIDE="C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/Common7/IDE/devenv.exe"

#check TARGETENVARCH
ifeq ($(TARGETENVARCH),)
    $(error TARGETENVARCH undefined.)
endif


#check BUILDTYPE 
ifeq ($(BUILDTYPE),)
    $(error BUILDTYPE undefined.)
endif

ifneq ($(BUILDTYPE), release)
   ifneq ($(BUILDTYPE), debug)
       $(error BUILDTYPE is error, set the value to debug or release.)
   endif
endif

#
ifeq ($(BIN_DIR),)
	BIN_DIR=$(BUILDTYPE)_win_$(TARGETENVARCH)
endif
BUILDOUTPUTDIR=$(NLBUILDROOT)/bin/$(BIN_DIR)

#
ifeq ($(VERSION_BUILD), )
	VERSION_BUILD=$(shell date +"%y.%j.%H%M")DX-$(HOSTNAME)-$(USERNAME)-$(shell date +"%Y.%m.%d-%H:%M")
endif


$(info --------------------------------------------------------------------------)
$(info [Targets])
$(info PROJECT=$(PROJECT))
$(info [Parameters])
$(info NLBUILDROOT=$(NLBUILDROOT))
$(info TARGETENVARCH=$(TARGETENVARCH))
$(info BUILDTYPE=$(BUILDTYPE))
$(info BUILDOUTPUTDIR=$(BUILDOUTPUTDIR))
$(info BIN_DIR=$(BIN_DIR))
$(info [VERSION])
$(info PRODUCT=$(VERSION_PRODUCT))
$(info RELEASE=$(VERSION_MAJOR).$(VERSION_MINOR).$(VERSION_MAINTENANCE).$(VERSION_PATCH) ($(VERSION_BUILD)))
$(info ---------------------------------------------------------------------------)


.PHONY: all
all: versionInfo $(TARGETS)

.PHONY: versionInfo
versionInfo:
	@if [ "$(RCSRC)" != "" ]; then \
		perl $(NLBUILDROOT)/build/updateVersionInfo_make.pl $(RCSRC) $(VERSION_MAJOR) $(VERSION_MINOR) $(VERSION_MAINTENANCE) $(VERSION_PATCH) "$(VERSION_BUILD)" "$(VERSION_PRODUCT)" $(TARGETENVARCH); \
		echo " --- Modified .rc file ---" ; \
		egrep "FILEVERSION|PRODUCTVERSION|CompanyName|FileDescription|FileVersion|LegalCopyright|ProductName|ProductVersion" $(RCSRC) ; \
	fi

.PHONY: $(TARGETS)
$(TARGETS): 
	@echo ""
	@echo "Building $(PROJECT) $(BUILDTYPE) $(TARGETENVARCH) for NextLabs Exchange PEP"	
	rm -rf  $(BUILD_LOGFILE)
	$(MSVSIDE) $(PROJECT) /build "$(BUILDTYPE)|$(TARGETENVARCH)" /project $(PROJECTNAME) /out "$(BUILD_LOGFILE)";
	@COMPILE_STATUS=$$? ;
	@if [ -f $(BUILD_LOGFILE) ] ; then				\
		echo ""; \
		cat $(BUILD_LOGFILE) ;						\
	else													\
		echo "INFO: Cannot find $(BUILD_LOGFILE)" ;	\
	fi ;													\
	exit $$COMPILE_STATUS
	@if [ ! -d $(BUILDOUTPUTDIR)/ ]; then					\
   		mkdir -p $(BUILDOUTPUTDIR)/ ;						\
	fi
	@if [ "$(PROJECT_TYPE)" == "CPP" ]; then \
		cp -pf $(PROJECT_DIR)/$(TARGETENVARCH)/$(BUILDTYPE)/$(RESULTDLL) $(BUILDOUTPUTDIR)/ ;  \
		cp -pf $(PROJECT_DIR)/$(TARGETENVARCH)/$(BUILDTYPE)/$(RESULTPDB) $(BUILDOUTPUTDIR)/ ;  \
	else                                                                                     \
		cp -pf $(PROJECT_DIR)/bin/$(TARGETENVARCH)/$(BUILDTYPE)/$(RESULTDLL) $(BUILDOUTPUTDIR)/ ; \
		cp -pf $(PROJECT_DIR)/bin/$(TARGETENVARCH)/$(BUILDTYPE)/$(RESULTPDB) $(BUILDOUTPUTDIR)/ ;  \
	fi

.PHONY: clean
clean:
	rm -rf  $(BUILD_LOGFILE)
	$(MSVSIDE) $(PROJECT) /clean "$(BUILDTYPE)|$(TARGETENVARCH)" /project $(PROJECTNAME)
	rm -f $(BUILDOUTPUTDIR)/$(RESULTDLL)
	rm -f $(BUILDOUTPUTDIR)/$(RESULTPDB)