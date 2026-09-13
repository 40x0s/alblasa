#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""يولّد ../Crack_Guide_x32dbg.pdf — المحتوى مقسّم على guide_part{1,2,3}.py."""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rtl_pdf import Doc
import guide_part1 as p1, guide_part2 as p2, guide_part3 as p3

OUT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Crack_Guide_x32dbg.pdf"))

def main():
    d = Doc("كسر حماية ميزان Pro بـ x32dbg")
    p1.cover_and_map(d); p1.ch1(d); p1.ch2(d); p1.ch3(d); p1.ch4(d)
    p2.ch5(d); p2.ch6(d); p2.ch7(d); p2.ch8(d); p2.ch9(d)
    p3.ch10(d); p3.ch11(d); p3.ch12(d); p3.ch13(d); p3.ch14(d); p3.appendices(d)
    d.set_title("كسر حماية ميزان Pro بـ x32dbg — من الصفر إلى المية")
    d.set_creator("فريق ميزان Pro — دفعة 2026")
    d.set_subject("دليل عملي للهندسة العكسية وورشة x32dbg")
    d.set_lang("ar")
    d.output(OUT)
    print("written:", OUT, "pages:", d.page_no())

if __name__ == "__main__":
    main()
