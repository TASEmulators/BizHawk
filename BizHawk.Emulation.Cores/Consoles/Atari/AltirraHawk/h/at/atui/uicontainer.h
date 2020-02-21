#ifndef f_AT2_UICONTAINER_H
#define f_AT2_UICONTAINER_H

#include <at/atui/uiwidget.h>

class ATUIContainer : public ATUIWidget {
public:
	ATUIContainer();
	~ATUIContainer();

	void AddChild(ATUIWidget *w);
	void RemoveChild(ATUIWidget *w);
	void RemoveAllChildren();

	void SendToBack(ATUIWidget *w);
	void BringToFront(ATUIWidget *w);

	void InvalidateLayout();
	void UpdateLayout();

	ATUIWidget *HitTest(vdpoint32 pt) override;

	void OnDestroy() override;
	void OnSize() override;

	void OnSetFocus() override;

	ATUIWidget *DragHitTest(vdpoint32 pt) override;

protected:
	void Paint(IVDDisplayRenderer& rdr, sint32 w, sint32 h) override;

	bool mbLayoutInvalid;
	bool mbDescendantLayoutInvalid;

	typedef vdfastvector<ATUIWidget *> Widgets;
	Widgets mWidgets;
};

#endif
