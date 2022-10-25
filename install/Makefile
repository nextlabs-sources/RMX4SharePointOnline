include $(NLBUILDROOT)/build/Makefile.ver

VERSION_STR=$(VERSION_MAJOR_RMX4SPO).$(VERSION_MINOR_RMX4SPO)

ASSEMBLY_DIR=assembly
PACKAGE_FILE=NextlabsRightsManagerForSharePointOnline-$(VERSION_STR)-$(VERSION_BUILD_SHORT)-$(shell date +"%Y%m%d%H%M").zip

DEST_INSTALL_DIR=$(NLBUILDROOT)/bin/install
.PHONY: all
all: installer

PHONY: installer
installer:
	@echo "INFO: Make installer"
	@if [ ! -d $(ASSEMBLY_DIR) ]; then					\
		mkdir $(ASSEMBLY_DIR) ;						\
	fi
	@if [ ! -d $(DEST_INSTALL_DIR) ]; then					\
		mkdir $(DEST_INSTALL_DIR) ;						\
	fi
	cp -pf $(NLBUILDROOT)/bin/release_win_x64/*  $(ASSEMBLY_DIR)/
	@echo "INFO: Zip assembly"
	cd $(ASSEMBLY_DIR); zip $(DEST_INSTALL_DIR)/$(PACKAGE_FILE) *