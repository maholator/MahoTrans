// Copyright (c) Fyodor Ryzhov / Arman Jussupgaliyev. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MahoTrans.Runtime;

namespace javax.microedition.lcdui;

public class ChoiceItem
{
    public Reference Text;
    public Reference Image;
    public Reference Font;

    public ChoiceItem(Reference text, Reference image)
    {
        Text = text;
        Image = image;
        Font = Reference.Null;
    }

    public void AnnounceHiddenReferences(Queue<Reference> queue)
    {
        queue.Enqueue(Text);
        queue.Enqueue(Image);
        queue.Enqueue(Font);
    }
}
