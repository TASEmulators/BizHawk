LANGS=de es fr it lt pt pt_BR sv

dist-hook:
	@for l in $(LANGS) ; do \
		cp -p "$(srcdir)/$(PACKAGE)_$$l.yts" "$(distdir)/$(PACKAGE)_$$l.yts" ; \
	done

install-data-hook:
	test -z "$(DESTDIR)$(datadir)/$(PACKAGE)/yts" || $(MKDIR_P) "$(DESTDIR)$(datadir)/$(PACKAGE)/yts"
	@for l in $(LANGS) ; do \
		echo " $(INSTALL_DATA) $(PACKAGE)_$$l.yts $(DESTDIR)$(datadir)/$(PACKAGE)/yts/$$l.yts" ; \
		$(INSTALL_DATA) "$(srcdir)/$(PACKAGE)_$$l.yts" "$(DESTDIR)$(datadir)/$(PACKAGE)/yts/$$l.yts" ; \
	done

uninstall-hook:
	@for l in $(LANGS) ; do \
		echo " rm -f $(DESTDIR)$(datadir)/$(PACKAGE)/yts/$$l.yts" ; \
		rm -f "$(DESTDIR)$(datadir)/$(PACKAGE)/yts/$$l.yts" ; \
	done
