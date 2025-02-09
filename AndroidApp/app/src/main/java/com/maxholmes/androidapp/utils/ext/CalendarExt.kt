package com.maxholmes.androidapp.utils.ext

import java.text.SimpleDateFormat
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Calendar
import java.util.Locale

fun Calendar.getDayOfMonth(): Int {
    return this.get(Calendar.DAY_OF_MONTH)
}

fun Calendar.getDayOfWeekName(): String {
    val dayOfWeek = this.get(Calendar.DAY_OF_WEEK)

    return when (dayOfWeek) {
        Calendar.SUNDAY -> "Chủ Nhật"
        Calendar.MONDAY -> "Thứ Hai"
        Calendar.TUESDAY -> "Thứ Ba"
        Calendar.WEDNESDAY -> "Thứ Tư"
        Calendar.THURSDAY -> "Thứ Năm"
        Calendar.FRIDAY -> "Thứ Sáu"
        Calendar.SATURDAY -> "Thứ Bảy"
        else -> "Không xác định"
    }
}

fun Calendar.formatDate(pattern: String = "yyyy-MM-dd"): String {
    val dateFormat = SimpleDateFormat(pattern, Locale.getDefault())
    return dateFormat.format(this.time)
}

fun Calendar.formatDateToShow(pattern: String = "dd/MM/yyyy"): String {
    val dateFormat = SimpleDateFormat(pattern, Locale.getDefault())
    return dateFormat.format(this.time)
}
fun String.formatDateToRequest(): String {
    val inputFormatter = DateTimeFormatter.ofPattern("dd/MM/yyyy", Locale.getDefault())
    val outputFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd", Locale.getDefault())
    return LocalDate.parse(this, inputFormatter).format(outputFormatter)
}


fun String.toCustomDateFormat(): String {
    val inputFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss")

    val outputFormatter = DateTimeFormatter.ofPattern("dd/MM/yyyy")

    val date = LocalDate.parse(this, inputFormatter)

    return date.format(outputFormatter)
}
fun String.toCustomDateTimeFormat(): String {
    val inputFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd'T'HH:mm:ss")

    val outputFormatter = DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss")

    val dateTime = LocalDateTime.parse(this, inputFormatter)

    return dateTime.format(outputFormatter)
}