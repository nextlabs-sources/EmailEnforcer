ifeq ($(NLBUILDROOT),)
  $(error NLBUILDROOT undefined.)
endif

ifeq ($(NLEXTERNALDIR),)
  $(error NLEXTERNALDIR undefined.)
endif

include $(NLBUILDROOT)/build/Makefile.ver

PRODUCT_DIR_NAME=ExchangeEnforcer
VERSION_STR=$(VERSION_MAJOR_SPOE).$(VERSION_MINOR_SPOE)
ISM_TEMPLATE_FILE_NAME_X86=ExchangeEnforcer.ism
ISM_TEMPLATE_FILE_NAME_X64=ExchangeEnforcer_x64.ism
ISM_RELEASE=msi
ISM_PRODUCT_CONFIG64=64bit
MSI_FILE64=CE-ExchangeEnforcer64.msi
TARGETMSI_FILE64_NAME=ExchangeEnforcer-setup64.msi

#ifeq ($(BUILDTYPE),)
#	BUILDTYPE=dev
#endif

ifeq ($(VERSION_BUILD),)
	VERSION_BUILD=1
endif

include $(NLBUILDROOT)/install/scripts/Makefile.install
